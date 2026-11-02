using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa;

public class ReclamosPostventaUseCase
{
    private static readonly string[] OrdenTipos = new[]
    {
        "Defecto",
        "Retraso",
        "Faltante",
        "Facturacion",
        "Otro"
    };

    private readonly IUnitOfWork _unitOfWork;

    public ReclamosPostventaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaReclamosDto> ListarAsync(
        string? busqueda,
        string? estado,
        string? tipo,
        string? prioridad,
        int? clienteId,
        bool? vencidos,
        int pagina,
        int tamanoPagina)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        string? estadoNormalizado = ValidacionReclamo.NormalizarEstadoFiltro(estado);
        string? tipoNormalizado = ValidacionReclamo.NormalizarTipoFiltro(tipo);
        string? prioridadNormalizada = ValidacionReclamo.NormalizarPrioridadFiltro(prioridad);
        DateTime ahora = FechasUtc.Normalizar(DateTime.UtcNow);
        IQueryable<ReclamoPostventa> consulta = _unitOfWork.Repository<ReclamoPostventa>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(reclamo => reclamo.Numero.ToLower().Contains(termino)
                || _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Any(orden => orden.Id == reclamo.OrdenCompraId
                        && orden.NumeroOrden.ToLower().Contains(termino))
                || _unitOfWork.Repository<Cliente>().AsQueryable()
                    .Any(cliente => cliente.Id == reclamo.ClienteId
                        && cliente.Nombre.ToLower().Contains(termino)));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(reclamo => reclamo.Estado == estadoNormalizado);
        }

        if (tipoNormalizado is not null)
        {
            consulta = consulta.Where(reclamo => reclamo.Tipo == tipoNormalizado);
        }

        if (prioridadNormalizada is not null)
        {
            consulta = consulta.Where(reclamo => reclamo.Prioridad == prioridadNormalizada);
        }

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(reclamo => reclamo.ClienteId == clienteId.Value);
        }

        if (vencidos == true)
        {
            consulta = consulta.Where(reclamo =>
                (reclamo.Estado == "Abierto" || reclamo.Estado == "EnProceso")
                && reclamo.FechaLimite < ahora);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<ReclamoPostventa> reclamos = await consulta
            .OrderBy(reclamo => reclamo.Prioridad == "Alta" ? 1 : reclamo.Prioridad == "Media" ? 2 : 3)
            .ThenBy(reclamo => reclamo.FechaLimite)
            .ThenBy(reclamo => reclamo.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .ToListAsync();
        List<ReclamoListadoDto> items = await ProyectarListadoAsync(reclamos, ahora);
        return new PaginaReclamosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ReclamoDetalleDto> ObtenerAsync(int id)
    {
        ReclamoPostventa? reclamo = await _unitOfWork.Repository<ReclamoPostventa>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (reclamo is null)
        {
            throw CrearNoEncontrado(id);
        }

        return await ProyectarDetalleAsync(reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }

    public async Task<ReclamoCreadoDto> CrearAsync(GuardarReclamoRequestDto solicitud)
    {
        GuardarReclamoRequestDto datosValidados = await ValidacionReclamo.ValidarAsync(solicitud, _unitOfWork);
        DateTime fecha = FechasUtc.Normalizar(DateTime.UtcNow);
        ReclamoPostventa reclamo = new ReclamoPostventa
        {
            Numero = CrearNumeroTemporal(),
            Fecha = fecha,
            ClienteId = datosValidados.ClienteId,
            OrdenCompraId = datosValidados.OrdenCompraId,
            ProductoId = datosValidados.ProductoId,
            Tipo = datosValidados.Tipo,
            Prioridad = datosValidados.Prioridad,
            Descripcion = datosValidados.Descripcion,
            Estado = "Abierto",
            FechaLimite = CalcularFechaLimite(fecha, datosValidados.Prioridad)
        };

        try
        {
            using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _unitOfWork.Repository<ReclamoPostventa>().AddAsync(reclamo);
                await _unitOfWork.CompleteAsync();
                reclamo.Numero = CrearNumero(reclamo.Id);
                _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
                await _unitOfWork.CompleteAsync();
                transaccion.Complete();
            }
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe el reclamo {CrearNumero(reclamo.Id)}.");
        }

        return new ReclamoCreadoDto
        {
            Id = reclamo.Id,
            Numero = reclamo.Numero
        };
    }

    public async Task<ReclamoCreadoDto> ActualizarAsync(int id, GuardarReclamoRequestDto solicitud)
    {
        GuardarReclamoRequestDto datosValidados = await ValidacionReclamo.ValidarAsync(solicitud, _unitOfWork);
        ReclamoPostventa reclamo = await ObtenerEntidadAsync(id);
        ExigirEstado(reclamo, "Abierto");
        reclamo.ClienteId = datosValidados.ClienteId;
        reclamo.OrdenCompraId = datosValidados.OrdenCompraId;
        reclamo.ProductoId = datosValidados.ProductoId;
        reclamo.Tipo = datosValidados.Tipo;
        reclamo.Prioridad = datosValidados.Prioridad;
        reclamo.Descripcion = datosValidados.Descripcion;
        reclamo.FechaLimite = CalcularFechaLimite(reclamo.Fecha, reclamo.Prioridad);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return CrearDtoCreado(reclamo);
    }

    public async Task<ReclamoDetalleDto> IniciarAsync(int id)
    {
        ReclamoPostventa reclamo = await ObtenerEntidadAsync(id);
        ExigirEstado(reclamo, "Abierto");
        reclamo.Estado = "EnProceso";
        reclamo.FechaInicio = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await ProyectarDetalleAsync(reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }

    public async Task<ReclamoDetalleDto> ResolverAsync(int id, ResolverReclamoRequestDto solicitud)
    {
        ReclamoPostventa reclamo = await ObtenerEntidadAsync(id);
        ExigirEstado(reclamo, "Abierto", "EnProceso");
        reclamo.Resolucion = ValidacionReclamo.NormalizarResolucion(solicitud.Resolucion);
        reclamo.Estado = "Resuelto";
        reclamo.FechaResolucion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await ProyectarDetalleAsync(reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }

    public async Task<ReclamoDetalleDto> CerrarAsync(int id)
    {
        ReclamoPostventa reclamo = await ObtenerEntidadAsync(id);
        ExigirEstado(reclamo, "Resuelto");
        reclamo.Estado = "Cerrado";
        reclamo.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await ProyectarDetalleAsync(reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }

    public async Task<ReclamoDetalleDto> RechazarAsync(int id, RechazarReclamoRequestDto solicitud)
    {
        ReclamoPostventa reclamo = await ObtenerEntidadAsync(id);
        ExigirEstado(reclamo, "Abierto", "EnProceso");
        reclamo.MotivoRechazo = ValidacionReclamo.NormalizarMotivo(solicitud.Motivo);
        reclamo.Estado = "Rechazado";
        reclamo.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await ProyectarDetalleAsync(reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }

    public async Task<ResumenReclamosDto> ResumenAsync()
    {
        DateTime ahora = FechasUtc.Normalizar(DateTime.UtcNow);
        List<ReclamoPostventa> reclamos = await _unitOfWork.Repository<ReclamoPostventa>()
            .AsQueryable()
            .ToListAsync();
        List<ReclamoPostventa> completados = reclamos
            .Where(reclamo => (reclamo.Estado == "Resuelto" || reclamo.Estado == "Cerrado")
                && reclamo.FechaResolucion.HasValue)
            .ToList();
        List<ReclamoPostventa> aTiempo = completados
            .Where(reclamo => reclamo.FechaResolucion!.Value <= reclamo.FechaLimite)
            .ToList();
        decimal horasPromedio = completados.Count == 0
            ? 0m
            : decimal.Round(completados.Average(reclamo =>
                (decimal)(reclamo.FechaResolucion!.Value - reclamo.Fecha).TotalHours), 2, MidpointRounding.AwayFromZero);
        decimal cumplimiento = completados.Count == 0
            ? 0m
            : decimal.Round(aTiempo.Count * 100m / completados.Count, 2, MidpointRounding.AwayFromZero);
        List<TipoReclamosDto> porTipo = new List<TipoReclamosDto>();
        foreach (string tipo in OrdenTipos)
        {
            int cantidad = reclamos.Count(reclamo => reclamo.Tipo == tipo);
            if (cantidad > 0)
            {
                porTipo.Add(new TipoReclamosDto { Tipo = tipo, Reclamos = cantidad });
            }
        }

        return new ResumenReclamosDto
        {
            Total = reclamos.Count,
            Abiertos = reclamos.Count(reclamo => reclamo.Estado == "Abierto"),
            EnProceso = reclamos.Count(reclamo => reclamo.Estado == "EnProceso"),
            Resueltos = reclamos.Count(reclamo => reclamo.Estado == "Resuelto"),
            Cerrados = reclamos.Count(reclamo => reclamo.Estado == "Cerrado"),
            Rechazados = reclamos.Count(reclamo => reclamo.Estado == "Rechazado"),
            Vencidos = reclamos.Count(reclamo => EsVencido(reclamo, ahora)),
            PorTipo = porTipo,
            HorasPromedioResolucion = horasPromedio,
            CumplimientoPlazo = cumplimiento
        };
    }

    private async Task<ReclamoPostventa> ObtenerEntidadAsync(int id)
    {
        ReclamoPostventa? reclamo = await _unitOfWork.Repository<ReclamoPostventa>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (reclamo is null)
        {
            throw CrearNoEncontrado(id);
        }

        return reclamo;
    }

    private async Task<ReclamoDetalleDto> ProyectarDetalleAsync(ReclamoPostventa reclamo, DateTime ahora)
    {
        ReclamoListadoDto listado = (await ProyectarListadoAsync(new List<ReclamoPostventa> { reclamo }, ahora))[0];
        return new ReclamoDetalleDto
        {
            Id = listado.Id,
            Numero = listado.Numero,
            Fecha = listado.Fecha,
            ClienteId = listado.ClienteId,
            ClienteNombre = listado.ClienteNombre,
            OrdenCompraId = listado.OrdenCompraId,
            NumeroOrden = listado.NumeroOrden,
            ProductoNombre = listado.ProductoNombre,
            Tipo = listado.Tipo,
            Prioridad = listado.Prioridad,
            Estado = listado.Estado,
            FechaLimite = listado.FechaLimite,
            Vencido = listado.Vencido,
            Descripcion = reclamo.Descripcion,
            Resolucion = reclamo.Resolucion,
            MotivoRechazo = reclamo.MotivoRechazo,
            FechaInicio = reclamo.FechaInicio,
            FechaResolucion = reclamo.FechaResolucion,
            FechaCierre = reclamo.FechaCierre,
            AtendidoATiempo = reclamo.FechaResolucion.HasValue
                ? reclamo.FechaResolucion.Value <= reclamo.FechaLimite
                : null
        };
    }

    private async Task<List<ReclamoListadoDto>> ProyectarListadoAsync(
        List<ReclamoPostventa> reclamos,
        DateTime ahora)
    {
        List<int> clientesIds = reclamos.Select(reclamo => reclamo.ClienteId).Distinct().ToList();
        List<int> ordenesIds = reclamos.Select(reclamo => reclamo.OrdenCompraId).Distinct().ToList();
        List<int> productosIds = reclamos.Where(reclamo => reclamo.ProductoId.HasValue)
            .Select(reclamo => reclamo.ProductoId!.Value)
            .Distinct()
            .ToList();
        List<Cliente> clientes = await _unitOfWork.Repository<Cliente>().AsQueryable()
            .Where(cliente => clientesIds.Contains(cliente.Id))
            .Select(cliente => new Cliente { Id = cliente.Id, Nombre = cliente.Nombre })
            .ToListAsync();
        List<OrdenCompra> ordenes = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => ordenesIds.Contains(orden.Id))
            .Select(orden => new OrdenCompra { Id = orden.Id, NumeroOrden = orden.NumeroOrden })
            .ToListAsync();
        List<Producto> productos = await _unitOfWork.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id))
            .Select(producto => new Producto { Id = producto.Id, Nombre = producto.Nombre })
            .ToListAsync();
        Dictionary<int, string> nombresClientes = clientes.ToDictionary(cliente => cliente.Id, cliente => cliente.Nombre);
        Dictionary<int, string> numerosOrdenes = ordenes.ToDictionary(orden => orden.Id, orden => orden.NumeroOrden);
        Dictionary<int, string> nombresProductos = productos.ToDictionary(producto => producto.Id, producto => producto.Nombre);
        List<ReclamoListadoDto> resultado = new List<ReclamoListadoDto>();
        foreach (ReclamoPostventa reclamo in reclamos)
        {
            resultado.Add(new ReclamoListadoDto
            {
                Id = reclamo.Id,
                Numero = reclamo.Numero,
                Fecha = reclamo.Fecha,
                ClienteId = reclamo.ClienteId,
                ClienteNombre = nombresClientes.GetValueOrDefault(reclamo.ClienteId, string.Empty),
                OrdenCompraId = reclamo.OrdenCompraId,
                NumeroOrden = numerosOrdenes.GetValueOrDefault(reclamo.OrdenCompraId, string.Empty),
                ProductoNombre = reclamo.ProductoId.HasValue
                    ? nombresProductos.GetValueOrDefault(reclamo.ProductoId.Value)
                    : null,
                Tipo = reclamo.Tipo,
                Prioridad = reclamo.Prioridad,
                Estado = reclamo.Estado,
                FechaLimite = reclamo.FechaLimite,
                Vencido = EsVencido(reclamo, ahora)
            });
        }

        return resultado;
    }

    private static bool EsVencido(ReclamoPostventa reclamo, DateTime ahora)
    {
        return (reclamo.Estado == "Abierto" || reclamo.Estado == "EnProceso")
            && reclamo.FechaLimite < ahora;
    }

    private static DateTime CalcularFechaLimite(DateTime fecha, string prioridad)
    {
        int dias = prioridad == "Alta" ? 2 : prioridad == "Media" ? 5 : 10;
        return FechasUtc.Normalizar(fecha.AddDays(dias));
    }

    private static string CrearNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    private static string CrearNumero(int id)
    {
        return $"REC-{id:D6}";
    }

    private static ReclamoCreadoDto CrearDtoCreado(ReclamoPostventa reclamo)
    {
        return new ReclamoCreadoDto { Id = reclamo.Id, Numero = reclamo.Numero };
    }

    private static void ExigirEstado(ReclamoPostventa reclamo, params string[] estadosPermitidos)
    {
        if (!estadosPermitidos.Contains(reclamo.Estado))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El reclamo {reclamo.Numero} está {ValidacionReclamo.EstadoLegible(reclamo.Estado)}.");
        }
    }

    private static ReglaNegocioException CrearNoEncontrado(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El reclamo {id} no existe.");
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
