using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales;

public class ContratosComercialesUseCase
{
    private static readonly string[] CondicionesOrdenadas = { "Contado", "Credito30", "Credito60", "Credito90" };

    private readonly IUnitOfWork _unitOfWork;

    public ContratosComercialesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaContratosDto> ListarAsync(
        string? busqueda, string? estado, int? clienteId, bool? vencidos, int pagina, int tamanoPagina)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        string[] estadosValidos = { "Borrador", "Vigente", "Finalizado", "Cancelado" };
        if (estadoNormalizado.Length > 0 && !estadosValidos.Any(valor =>
            string.Equals(valor, estadoNormalizado, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ReglaNegocioException(400, "El estado no es válido.");
        }

        IQueryable<ContratoComercial> consulta = _unitOfWork.Repository<ContratoComercial>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (busquedaNormalizada.Length > 0)
        {
            consulta = consulta.Where(contrato => contrato.Numero.ToLower().Contains(busquedaNormalizada)
                || contrato.Titulo.ToLower().Contains(busquedaNormalizada)
                || contrato.Cliente.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(contrato => contrato.Estado.ToLower() == estadoNormalizado.ToLower());
        }

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(contrato => contrato.ClienteId == clienteId.Value);
        }

        if (vencidos == true)
        {
            DateTime hoy = DateTime.UtcNow.Date;
            consulta = consulta.Where(contrato => contrato.Estado == "Vigente" && contrato.FechaFin < hoy);
        }

        int total = await consulta.CountAsync();
        List<ContratoProyeccionDto> proyecciones = await consulta
            .OrderByDescending(contrato => contrato.FechaInicio)
            .ThenByDescending(contrato => contrato.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(contrato => new ContratoProyeccionDto
            {
                Id = contrato.Id,
                Numero = contrato.Numero,
                ClienteId = contrato.ClienteId,
                ClienteNombre = contrato.Cliente.Nombre,
                Titulo = contrato.Titulo,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                Estado = contrato.Estado,
                MontoComprometido = contrato.MontoComprometido,
                MontoConsumido = _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Where(orden => orden.ClienteId == contrato.ClienteId
                        && orden.Fecha >= contrato.FechaInicio
                        && orden.Fecha < contrato.FechaFin.AddDays(1))
                    .Sum(orden => (decimal?)orden.Total) ?? 0m
            })
            .ToListAsync();
        // El consumo se resuelve mediante una subconsulta correlacionada, sin consultas por contrato.

        List<ContratoListadoDto> items = proyecciones.Select(proyeccion => MapearListado(proyeccion)).ToList();
        return new PaginaContratosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ContratoDetalleDto> ObtenerAsync(int id)
    {
        ContratoComercial contrato = await ObtenerEntidadAsync(id);
        ContratoProyeccionDto proyeccion = await ProyectarAsync(contrato);
        List<OrdenPeriodoDto> ordenes = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => orden.ClienteId == contrato.ClienteId
                && orden.Fecha >= contrato.FechaInicio
                && orden.Fecha < contrato.FechaFin.AddDays(1))
            .OrderByDescending(orden => orden.Fecha)
            .Select(orden => new OrdenPeriodoDto
            {
                OrdenId = orden.Id,
                NumeroOrden = orden.NumeroOrden,
                Fecha = orden.Fecha,
                Total = orden.Total
            })
            .ToListAsync();
        return MapearDetalle(proyeccion, ordenes);
    }

    public async Task<ContratoCreadoDto> CrearAsync(GuardarContratoRequestDto solicitud)
    {
        GuardarContratoRequestDto datos = await ValidacionContratoComercial.ValidarAsync(solicitud, _unitOfWork);
        ContratoComercial contrato = CrearEntidad(datos);
        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            contrato.Numero = $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
            await _unitOfWork.Repository<ContratoComercial>().AddAsync(contrato);
            await _unitOfWork.CompleteAsync();
            contrato.Numero = CrearNumero(contrato.Id);
            _unitOfWork.Repository<ContratoComercial>().Update(contrato);
            await _unitOfWork.CompleteAsync();
            transaccion.Complete();
        }

        return new ContratoCreadoDto { Id = contrato.Id, Numero = contrato.Numero };
    }

    public async Task<ContratoCreadoDto> ActualizarAsync(int id, GuardarContratoRequestDto solicitud)
    {
        GuardarContratoRequestDto datos = await ValidacionContratoComercial.ValidarAsync(solicitud, _unitOfWork);
        ContratoComercial contrato = await ObtenerEntidadAsync(id);
        ValidarEstado(contrato, "Borrador");
        AplicarDatos(contrato, datos);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return new ContratoCreadoDto { Id = contrato.Id, Numero = contrato.Numero };
    }

    public async Task<ContratoDetalleDto> ActivarAsync(int id)
    {
        ContratoComercial contrato = await ObtenerEntidadAsync(id);
        ValidarEstado(contrato, "Borrador");
        if (contrato.FechaFin < DateTime.UtcNow.Date)
        {
            throw new ReglaNegocioException(409, $"El contrato {contrato.Numero} ya terminó su periodo.");
        }

        bool solapado = await _unitOfWork.Repository<ContratoComercial>().AsQueryable()
            .AnyAsync(otro => otro.Id != contrato.Id && otro.ClienteId == contrato.ClienteId
                && otro.Estado == "Vigente" && otro.FechaInicio <= contrato.FechaFin
                && otro.FechaFin >= contrato.FechaInicio);
        if (solapado)
        {
            ContratoComercial otro = await _unitOfWork.Repository<ContratoComercial>().AsQueryable()
                .Where(entidad => entidad.Id != contrato.Id && entidad.ClienteId == contrato.ClienteId
                    && entidad.Estado == "Vigente" && entidad.FechaInicio <= contrato.FechaFin
                    && entidad.FechaFin >= contrato.FechaInicio)
                .OrderBy(entidad => entidad.FechaInicio)
                .FirstAsync();
            throw new ReglaNegocioException(409,
                $"El cliente ya tiene el contrato vigente {otro.Numero} en ese periodo.");
        }

        contrato.Estado = "Vigente";
        contrato.FechaActivacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return await ObtenerAsync(id);
    }

    public async Task<ContratoDetalleDto> FinalizarAsync(int id)
    {
        ContratoComercial contrato = await ObtenerEntidadAsync(id);
        ValidarEstado(contrato, "Vigente");
        contrato.Estado = "Finalizado";
        contrato.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return await ObtenerAsync(id);
    }

    public async Task<ContratoDetalleDto> CancelarAsync(int id, CancelarContratoRequestDto solicitud)
    {
        ContratoComercial contrato = await ObtenerEntidadAsync(id);
        if (contrato.Estado != "Borrador" && contrato.Estado != "Vigente")
        {
            throw ConflictoEstado(contrato);
        }

        string motivo = ValidacionContratoComercial.ValidarMotivo(solicitud.Motivo);
        contrato.Estado = "Cancelado";
        contrato.MotivoCancelacion = motivo;
        contrato.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return await ObtenerAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        ContratoComercial contrato = await ObtenerEntidadAsync(id);
        ValidarEstado(contrato, "Borrador");
        _unitOfWork.Repository<ContratoComercial>().Remove(contrato);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<ResumenContratosDto> ResumenAsync()
    {
        List<ContratoComercial> contratos = await _unitOfWork.Repository<ContratoComercial>()
            .AsQueryable().ToListAsync();
        List<ContratoProyeccionDto> vigentes = await _unitOfWork.Repository<ContratoComercial>()
            .AsQueryable()
            .Where(contrato => contrato.Estado == "Vigente")
            .Select(contrato => new ContratoProyeccionDto
            {
                Id = contrato.Id,
                Numero = contrato.Numero,
                ClienteId = contrato.ClienteId,
                ClienteNombre = contrato.Cliente.Nombre,
                Titulo = contrato.Titulo,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                Estado = contrato.Estado,
                MontoComprometido = contrato.MontoComprometido,
                MontoConsumido = _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Where(orden => orden.ClienteId == contrato.ClienteId
                        && orden.Fecha >= contrato.FechaInicio && orden.Fecha < contrato.FechaFin.AddDays(1))
                    .Sum(orden => (decimal?)orden.Total) ?? 0m
            })
            .ToListAsync();
        decimal comprometido = vigentes.Sum(contrato => contrato.MontoComprometido);
        decimal consumido = vigentes.Sum(contrato => contrato.MontoConsumido);

        List<CondicionContratosDto> porCondicion = new List<CondicionContratosDto>();
        foreach (string condicion in CondicionesOrdenadas)
        {
            int cantidad = contratos.Count(contrato => contrato.CondicionPago == condicion);
            if (cantidad > 0)
            {
                porCondicion.Add(new CondicionContratosDto { CondicionPago = condicion, Contratos = cantidad });
            }
        }

        return new ResumenContratosDto
        {
            Total = contratos.Count,
            Borradores = contratos.Count(contrato => contrato.Estado == "Borrador"),
            Vigentes = vigentes.Count,
            Vencidos = vigentes.Count(contrato => contrato.FechaFin < DateTime.UtcNow.Date),
            Finalizados = contratos.Count(contrato => contrato.Estado == "Finalizado"),
            Cancelados = contratos.Count(contrato => contrato.Estado == "Cancelado"),
            MontoComprometidoVigente = comprometido,
            MontoConsumidoVigente = consumido,
            PorcentajeConsumoVigente = comprometido == 0m ? 0m : Math.Round(consumido / comprometido * 100m, 2),
            PorCondicion = porCondicion
        };
    }

    private async Task<ContratoComercial> ObtenerEntidadAsync(int id)
    {
        ContratoComercial? contrato = await _unitOfWork.Repository<ContratoComercial>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (contrato is null)
        {
            throw new ReglaNegocioException(404, $"El contrato {id} no existe.");
        }

        return contrato;
    }

    private async Task<ContratoProyeccionDto> ProyectarAsync(ContratoComercial contrato)
    {
        return await _unitOfWork.Repository<ContratoComercial>().AsQueryable()
            .Where(entidad => entidad.Id == contrato.Id)
            .Select(entidad => new ContratoProyeccionDto
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                ClienteId = entidad.ClienteId,
                ClienteNombre = entidad.Cliente.Nombre,
                Titulo = entidad.Titulo,
                FechaInicio = entidad.FechaInicio,
                FechaFin = entidad.FechaFin,
                Estado = entidad.Estado,
                MontoComprometido = entidad.MontoComprometido,
                CondicionPago = entidad.CondicionPago,
                Observaciones = entidad.Observaciones,
                MotivoCancelacion = entidad.MotivoCancelacion,
                FechaActivacion = entidad.FechaActivacion,
                FechaCierre = entidad.FechaCierre,
                MontoConsumido = _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Where(orden => orden.ClienteId == entidad.ClienteId
                        && orden.Fecha >= entidad.FechaInicio && orden.Fecha < entidad.FechaFin.AddDays(1))
                    .Sum(orden => (decimal?)orden.Total) ?? 0m
            })
            .SingleAsync();
    }

    private static ContratoComercial CrearEntidad(GuardarContratoRequestDto datos)
    {
        ContratoComercial contrato = new ContratoComercial();
        AplicarDatos(contrato, datos);
        contrato.Estado = "Borrador";
        return contrato;
    }

    private static void AplicarDatos(ContratoComercial contrato, GuardarContratoRequestDto datos)
    {
        contrato.ClienteId = datos.ClienteId;
        contrato.Titulo = datos.Titulo;
        contrato.FechaInicio = datos.FechaInicio;
        contrato.FechaFin = datos.FechaFin;
        contrato.MontoComprometido = datos.MontoComprometido;
        contrato.CondicionPago = datos.CondicionPago;
        contrato.Observaciones = datos.Observaciones;
    }

    private static void ValidarEstado(ContratoComercial contrato, string esperado)
    {
        if (contrato.Estado != esperado)
        {
            throw ConflictoEstado(contrato);
        }
    }

    private static ReglaNegocioException ConflictoEstado(ContratoComercial contrato)
    {
        return new ReglaNegocioException(409,
            $"El contrato {contrato.Numero} ya está {contrato.Estado.ToLowerInvariant()}.");
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(400, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(400, "El tamaño de página debe estar entre 1 y 100.");
        }
    }

    private static string CrearNumero(int id)
    {
        return $"CC-{id:D6}";
    }

    private static ContratoListadoDto MapearListado(ContratoProyeccionDto proyeccion)
    {
        return new ContratoListadoDto
        {
            Id = proyeccion.Id,
            Numero = proyeccion.Numero,
            ClienteId = proyeccion.ClienteId,
            ClienteNombre = proyeccion.ClienteNombre,
            Titulo = proyeccion.Titulo,
            FechaInicio = proyeccion.FechaInicio,
            FechaFin = proyeccion.FechaFin,
            Estado = proyeccion.Estado,
            Vencido = proyeccion.Estado == "Vigente" && proyeccion.FechaFin < DateTime.UtcNow.Date,
            MontoComprometido = proyeccion.MontoComprometido,
            MontoConsumido = proyeccion.MontoConsumido,
            PorcentajeConsumido = Math.Round(proyeccion.MontoConsumido / proyeccion.MontoComprometido * 100m, 2)
        };
    }

    private static ContratoDetalleDto MapearDetalle(ContratoProyeccionDto proyeccion, List<OrdenPeriodoDto> ordenes)
    {
        ContratoListadoDto listado = MapearListado(proyeccion);
        return new ContratoDetalleDto
        {
            Id = listado.Id,
            Numero = listado.Numero,
            ClienteId = listado.ClienteId,
            ClienteNombre = listado.ClienteNombre,
            Titulo = listado.Titulo,
            FechaInicio = listado.FechaInicio,
            FechaFin = listado.FechaFin,
            Estado = listado.Estado,
            Vencido = listado.Vencido,
            MontoComprometido = listado.MontoComprometido,
            MontoConsumido = listado.MontoConsumido,
            PorcentajeConsumido = listado.PorcentajeConsumido,
            CondicionPago = proyeccion.CondicionPago,
            Observaciones = proyeccion.Observaciones,
            MotivoCancelacion = proyeccion.MotivoCancelacion,
            FechaActivacion = proyeccion.FechaActivacion,
            FechaCierre = proyeccion.FechaCierre,
            Saldo = proyeccion.MontoComprometido - proyeccion.MontoConsumido,
            OrdenesPeriodo = ordenes
        };
    }
}
