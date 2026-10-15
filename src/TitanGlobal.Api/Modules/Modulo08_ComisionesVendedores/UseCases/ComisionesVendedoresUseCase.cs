using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo08_ComisionesVendedores.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo08_ComisionesVendedores.UseCases;

public class ComisionesVendedoresUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ComisionesVendedoresUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaVendedoresDto> ListarVendedoresAsync(
        string? busqueda,
        bool? activo,
        int pagina,
        int tamanoPagina)
    {
        ValidacionComisiones.ValidarPaginacion(pagina, tamanoPagina);
        IQueryable<Vendedor> consulta = _unitOfWork.Repository<Vendedor>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(vendedor => vendedor.Codigo.ToLower().Contains(termino)
                || vendedor.Nombre.ToLower().Contains(termino));
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(vendedor => vendedor.Activo == activo.Value);
        }

        int total = await consulta.CountAsync();
        List<VendedorListadoDto> items = await consulta
            .OrderBy(vendedor => vendedor.Codigo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(vendedor => new VendedorListadoDto
            {
                Id = vendedor.Id,
                Codigo = vendedor.Codigo,
                Nombre = vendedor.Nombre,
                PorcentajeComision = vendedor.PorcentajeComision,
                Activo = vendedor.Activo,
                OrdenesAsignadas = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Count(asignacion => asignacion.VendedorId == vendedor.Id)
            })
            .ToListAsync();
        return new PaginaVendedoresDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    public async Task<VendedorDetalleDto> ObtenerVendedorAsync(int id)
    {
        Vendedor vendedor = await ObtenerVendedorEntidadAsync(id);
        List<int> ordenesIds = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .Where(asignacion => asignacion.VendedorId == id)
            .Select(asignacion => asignacion.OrdenCompraId)
            .ToListAsync();
        decimal ventas = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => ordenesIds.Contains(orden.Id))
            .SumAsync(orden => (decimal?)orden.Total) ?? 0m;
        int cantidad = ordenesIds.Count;
        return new VendedorDetalleDto
        {
            Id = vendedor.Id,
            Codigo = vendedor.Codigo,
            Nombre = vendedor.Nombre,
            PorcentajeComision = vendedor.PorcentajeComision,
            Activo = vendedor.Activo,
            Email = vendedor.Email,
            OrdenesAsignadas = cantidad,
            VentasTotales = ventas
        };
    }

    public async Task<IdDto> CrearVendedorAsync(GuardarVendedorRequestDto solicitud)
    {
        GuardarVendedorRequestDto datos = ValidacionComisiones.ValidarVendedor(solicitud);
        await ValidarCodigoUnicoAsync(datos.Codigo, null);
        Vendedor vendedor = CrearEntidad(datos);
        await _unitOfWork.Repository<Vendedor>().AddAsync(vendedor);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw new ReglaNegocioException(409, $"Ya existe un vendedor con el código {datos.Codigo}.");
        }

        return new IdDto { Id = vendedor.Id };
    }

    public async Task<IdDto> ActualizarVendedorAsync(int id, GuardarVendedorRequestDto solicitud)
    {
        GuardarVendedorRequestDto datos = ValidacionComisiones.ValidarVendedor(solicitud);
        Vendedor vendedor = await ObtenerVendedorEntidadAsync(id);
        await ValidarCodigoUnicoAsync(datos.Codigo, id);
        AplicarDatos(vendedor, datos);
        _unitOfWork.Repository<Vendedor>().Update(vendedor);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw new ReglaNegocioException(409, $"Ya existe un vendedor con el código {datos.Codigo}.");
        }

        return new IdDto { Id = vendedor.Id };
    }

    public async Task EliminarVendedorAsync(int id)
    {
        Vendedor vendedor = await ObtenerVendedorEntidadAsync(id);
        bool tieneAsignaciones = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .AnyAsync(asignacion => asignacion.VendedorId == id);
        bool tieneLiquidaciones = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .AnyAsync(liquidacion => liquidacion.VendedorId == id);
        if (tieneAsignaciones || tieneLiquidaciones)
        {
            throw new ReglaNegocioException(
                409,
                $"No se puede eliminar el vendedor {vendedor.Codigo} porque tiene ventas asignadas.");
        }

        _unitOfWork.Repository<Vendedor>().Remove(vendedor);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<PaginaOrdenesVendedorDto> ListarOrdenesAsync(
        int? vendedorId,
        bool? sinAsignar,
        int? anio,
        int? mes,
        int pagina,
        int tamanoPagina)
    {
        ValidacionComisiones.ValidarPaginacion(pagina, tamanoPagina);
        if (mes.HasValue && (mes.Value < 1 || mes.Value > 12))
        {
            throw new ReglaNegocioException(400, "El mes no es válido.");
        }

        IQueryable<OrdenCompra> ordenes = _unitOfWork.Repository<OrdenCompra>().AsQueryable();
        if (anio.HasValue)
        {
            ordenes = ordenes.Where(orden => orden.Fecha.Year == anio.Value);
        }

        if (mes.HasValue)
        {
            ordenes = ordenes.Where(orden => orden.Fecha.Month == mes.Value);
        }

        if (vendedorId.HasValue)
        {
            ordenes = ordenes.Where(orden => _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                .Any(asignacion => asignacion.OrdenCompraId == orden.Id && asignacion.VendedorId == vendedorId.Value));
        }

        if (sinAsignar == true)
        {
            ordenes = ordenes.Where(orden => !_unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                .Any(asignacion => asignacion.OrdenCompraId == orden.Id));
        }

        int total = await ordenes.CountAsync();
        List<OrdenVendedorDto> items = await ordenes
            .OrderByDescending(orden => orden.Fecha)
            .ThenByDescending(orden => orden.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(orden => new OrdenVendedorDto
            {
                OrdenCompraId = orden.Id,
                NumeroOrden = orden.NumeroOrden,
                Fecha = orden.Fecha,
                ClienteNombre = orden.Cliente.Nombre,
                Total = orden.Total,
                VendedorId = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == orden.Id)
                    .Select(asignacion => (int?)asignacion.VendedorId)
                    .FirstOrDefault(),
                VendedorCodigo = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == orden.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Codigo)
                        .FirstOrDefault())
                    .FirstOrDefault(),
                VendedorNombre = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == orden.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Nombre)
                        .FirstOrDefault())
                    .FirstOrDefault()
            })
            .ToListAsync();
        return new PaginaOrdenesVendedorDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    public async Task<OrdenVendedorDto> AsignarVendedorAsync(int ordenCompraId, int vendedorId)
    {
        OrdenCompra? orden = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == ordenCompraId);
        if (orden is null)
        {
            throw new ReglaNegocioException(404, $"La orden de compra {ordenCompraId} no existe.");
        }

        Vendedor vendedor = await ObtenerVendedorParaAsignarAsync(vendedorId);
        await ValidarMesNoLiquidadoAsync(orden.Fecha);
        AsignacionVenta? asignacion = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.OrdenCompraId == ordenCompraId);
        if (asignacion is null)
        {
            asignacion = new AsignacionVenta
            {
                OrdenCompraId = ordenCompraId,
                VendedorId = vendedor.Id,
                FechaAsignacion = FechasUtc.Normalizar(DateTime.UtcNow)
            };
            await _unitOfWork.Repository<AsignacionVenta>().AddAsync(asignacion);
        }
        else
        {
            asignacion.VendedorId = vendedor.Id;
            asignacion.FechaAsignacion = FechasUtc.Normalizar(DateTime.UtcNow);
            _unitOfWork.Repository<AsignacionVenta>().Update(asignacion);
        }

        await _unitOfWork.CompleteAsync();
        return await ObtenerOrdenAsync(ordenCompraId);
    }

    public async Task QuitarAsignacionAsync(int ordenCompraId)
    {
        OrdenCompra? orden = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == ordenCompraId);
        if (orden is null)
        {
            throw new ReglaNegocioException(404, $"La orden de compra {ordenCompraId} no existe.");
        }

        await ValidarMesNoLiquidadoAsync(orden.Fecha);
        AsignacionVenta? asignacion = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.OrdenCompraId == ordenCompraId);
        if (asignacion is null)
        {
            throw new ReglaNegocioException(404, $"La orden {orden.NumeroOrden} no tiene vendedor.");
        }

        _unitOfWork.Repository<AsignacionVenta>().Remove(asignacion);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<ComisionesMesDto> ObtenerComisionesAsync(int anio, int mes)
    {
        ValidacionComisiones.ValidarPeriodo(anio, mes);
        List<LiquidacionComision> liquidaciones = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .Where(liquidacion => liquidacion.Anio == anio && liquidacion.Mes == mes)
            .ToListAsync();
        if (liquidaciones.Count > 0)
        {
            List<int> vendedoresIds = liquidaciones.Select(liquidacion => liquidacion.VendedorId).ToList();
            List<Vendedor> vendedores = await _unitOfWork.Repository<Vendedor>().AsQueryable()
                .Where(vendedor => vendedoresIds.Contains(vendedor.Id)).ToListAsync();
            List<ComisionVendedorDto> filasGuardadas = liquidaciones
                .Select(liquidacion => MapearComisionLiquidada(liquidacion, vendedores.Single(vendedor => vendedor.Id == liquidacion.VendedorId)))
                .OrderBy(fila => fila.Codigo)
                .ToList();
            return CrearResumenComisiones(anio, mes, true, filasGuardadas);
        }

        (DateTime inicio, DateTime fin) = LimitesMes(anio, mes);
        // PATRÓN LINQ 4: GroupBy y Sum agrupan ventas y órdenes por vendedor en una sola consulta.
        List<ComisionAgrupada> agrupadas = await (
            from asignacion in _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            join orden in _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                on asignacion.OrdenCompraId equals orden.Id
            join vendedor in _unitOfWork.Repository<Vendedor>().AsQueryable()
                on asignacion.VendedorId equals vendedor.Id
            where orden.Fecha >= inicio && orden.Fecha < fin
            group new { orden, vendedor } by new { vendedor.Id, vendedor.Codigo, vendedor.Nombre, vendedor.PorcentajeComision } into grupo
            select new ComisionAgrupada
            {
                VendedorId = grupo.Key.Id,
                Codigo = grupo.Key.Codigo,
                Nombre = grupo.Key.Nombre,
                Ordenes = grupo.Count(),
                Ventas = grupo.Sum(item => item.orden.Total),
                Porcentaje = grupo.Key.PorcentajeComision
            }).ToListAsync();
        List<ComisionVendedorDto> filas = agrupadas.Select(grupo => new ComisionVendedorDto
        {
            VendedorId = grupo.VendedorId,
            Codigo = grupo.Codigo,
            Nombre = grupo.Nombre,
            Ordenes = grupo.Ordenes,
            Ventas = grupo.Ventas,
            Porcentaje = grupo.Porcentaje,
            Comision = Math.Round(grupo.Ventas * grupo.Porcentaje / 100m, 2, MidpointRounding.AwayFromZero),
            Estado = "Pendiente"
        }).OrderBy(fila => fila.Codigo).ToList();
        return CrearResumenComisiones(anio, mes, false, filas);
    }

    public async Task<ComisionesMesDto> LiquidarMesAsync(int anio, int mes)
    {
        ValidacionComisiones.ValidarPeriodo(anio, mes);
        (DateTime inicio, DateTime fin) = LimitesMes(anio, mes);
        bool yaLiquidado = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .AnyAsync(liquidacion => liquidacion.Anio == anio && liquidacion.Mes == mes);
        if (yaLiquidado)
        {
            throw new ReglaNegocioException(409, $"El mes {mes:D2}/{anio} ya está liquidado.");
        }

        List<ComisionAgrupada> agrupadas = await (
            from asignacion in _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            join orden in _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                on asignacion.OrdenCompraId equals orden.Id
            join vendedor in _unitOfWork.Repository<Vendedor>().AsQueryable()
                on asignacion.VendedorId equals vendedor.Id
            where orden.Fecha >= inicio && orden.Fecha < fin
            group new { orden, vendedor } by new { vendedor.Id, vendedor.Codigo, vendedor.Nombre, vendedor.PorcentajeComision } into grupo
            select new ComisionAgrupada
            {
                VendedorId = grupo.Key.Id,
                Codigo = grupo.Key.Codigo,
                Nombre = grupo.Key.Nombre,
                Ordenes = grupo.Count(),
                Ventas = grupo.Sum(item => item.orden.Total),
                Porcentaje = grupo.Key.PorcentajeComision
            }).ToListAsync();
        if (agrupadas.Count == 0)
        {
            throw new ReglaNegocioException(400, $"No hay ventas asignadas en {mes:D2}/{anio}.");
        }

        List<LiquidacionComision> nuevas = agrupadas.Select(grupo => new LiquidacionComision
        {
            VendedorId = grupo.VendedorId,
            Anio = anio,
            Mes = mes,
            Ordenes = grupo.Ordenes,
            Ventas = grupo.Ventas,
            Porcentaje = grupo.Porcentaje,
            Comision = Math.Round(grupo.Ventas * grupo.Porcentaje / 100m, 2, MidpointRounding.AwayFromZero),
            Estado = "Calculada",
            FechaCalculo = FechasUtc.Normalizar(DateTime.UtcNow)
        }).ToList();
        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            foreach (LiquidacionComision liquidacion in nuevas)
            {
                await _unitOfWork.Repository<LiquidacionComision>().AddAsync(liquidacion);
            }

            try
            {
                await _unitOfWork.CompleteAsync();
                transaccion.Complete();
            }
            catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
            {
                throw new ReglaNegocioException(409, $"El mes {mes:D2}/{anio} ya está liquidado.");
            }
        }

        return await ObtenerComisionesAsync(anio, mes);
    }

    public async Task<LiquidacionDto> PagarLiquidacionAsync(int id)
    {
        LiquidacionComision? liquidacion = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (liquidacion is null)
        {
            throw new ReglaNegocioException(404, $"La liquidación {id} no existe.");
        }

        if (liquidacion.Estado == "Pagada")
        {
            throw new ReglaNegocioException(409, $"La liquidación {id} ya está pagada.");
        }

        liquidacion.Estado = "Pagada";
        liquidacion.FechaPago = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<LiquidacionComision>().Update(liquidacion);
        await _unitOfWork.CompleteAsync();
        Vendedor vendedor = await ObtenerVendedorEntidadAsync(liquidacion.VendedorId);
        return MapearLiquidacion(liquidacion, vendedor);
    }

    private async Task<Vendedor> ObtenerVendedorEntidadAsync(int id)
    {
        Vendedor? vendedor = await _unitOfWork.Repository<Vendedor>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (vendedor is null)
        {
            throw new ReglaNegocioException(404, $"El vendedor {id} no existe.");
        }

        return vendedor;
    }

    private async Task<Vendedor> ObtenerVendedorParaAsignarAsync(int id)
    {
        Vendedor? vendedor = await _unitOfWork.Repository<Vendedor>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (vendedor is null)
        {
            throw new ReglaNegocioException(400, $"El vendedor {id} no existe.");
        }

        if (!vendedor.Activo)
        {
            throw new ReglaNegocioException(400, $"El vendedor {vendedor.Codigo} está inactivo.");
        }

        return vendedor;
    }

    private async Task ValidarCodigoUnicoAsync(string codigo, int? id)
    {
        bool repetido = await _unitOfWork.Repository<Vendedor>().AsQueryable()
            .AnyAsync(vendedor => vendedor.Codigo == codigo && (!id.HasValue || vendedor.Id != id.Value));
        if (repetido)
        {
            throw new ReglaNegocioException(409, $"Ya existe un vendedor con el código {codigo}.");
        }
    }

    private async Task ValidarMesNoLiquidadoAsync(DateTime fecha)
    {
        DateTime fechaUtc = FechasUtc.Normalizar(fecha);
        int anio = fechaUtc.Year;
        int mes = fechaUtc.Month;
        bool liquidado = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .AnyAsync(liquidacion => liquidacion.Anio == anio && liquidacion.Mes == mes);
        if (liquidado)
        {
            throw new ReglaNegocioException(
                409,
                $"El mes {mes:D2}/{anio} ya está liquidado; no se pueden cambiar sus asignaciones.");
        }
    }

    private async Task<OrdenVendedorDto> ObtenerOrdenAsync(int ordenCompraId)
    {
        OrdenVendedorDto? orden = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(entidad => entidad.Id == ordenCompraId)
            .Select(entidad => new OrdenVendedorDto
            {
                OrdenCompraId = entidad.Id,
                NumeroOrden = entidad.NumeroOrden,
                Fecha = entidad.Fecha,
                ClienteNombre = entidad.Cliente.Nombre,
                Total = entidad.Total,
                VendedorId = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == entidad.Id)
                    .Select(asignacion => (int?)asignacion.VendedorId)
                    .FirstOrDefault(),
                VendedorCodigo = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == entidad.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Codigo)
                        .FirstOrDefault())
                    .FirstOrDefault(),
                VendedorNombre = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == entidad.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Nombre)
                        .FirstOrDefault())
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync();
        if (orden is null)
        {
            throw new ReglaNegocioException(404, $"La orden de compra {ordenCompraId} no existe.");
        }

        return orden;
    }

    private static Vendedor CrearEntidad(GuardarVendedorRequestDto datos)
    {
        Vendedor vendedor = new Vendedor();
        AplicarDatos(vendedor, datos);
        return vendedor;
    }

    private static void AplicarDatos(Vendedor vendedor, GuardarVendedorRequestDto datos)
    {
        vendedor.Codigo = datos.Codigo;
        vendedor.Nombre = datos.Nombre;
        vendedor.Email = datos.Email;
        vendedor.PorcentajeComision = datos.PorcentajeComision;
        vendedor.Activo = datos.Activo;
    }

    private static ComisionesMesDto CrearResumenComisiones(
        int anio,
        int mes,
        bool liquidado,
        List<ComisionVendedorDto> filas)
    {
        return new ComisionesMesDto
        {
            Anio = anio,
            Mes = mes,
            Liquidado = liquidado,
            Filas = filas,
            TotalVentas = filas.Sum(fila => fila.Ventas),
            TotalComision = filas.Sum(fila => fila.Comision)
        };
    }

    private static ComisionVendedorDto MapearComisionLiquidada(LiquidacionComision liquidacion, Vendedor vendedor)
    {
        return new ComisionVendedorDto
        {
            VendedorId = liquidacion.VendedorId,
            Codigo = vendedor.Codigo,
            Nombre = vendedor.Nombre,
            Ordenes = liquidacion.Ordenes,
            Ventas = liquidacion.Ventas,
            Porcentaje = liquidacion.Porcentaje,
            Comision = liquidacion.Comision,
            Estado = liquidacion.Estado,
            LiquidacionId = liquidacion.Id
        };
    }

    private static LiquidacionDto MapearLiquidacion(LiquidacionComision liquidacion, Vendedor vendedor)
    {
        return new LiquidacionDto
        {
            Id = liquidacion.Id,
            VendedorId = liquidacion.VendedorId,
            Codigo = vendedor.Codigo,
            Nombre = vendedor.Nombre,
            Anio = liquidacion.Anio,
            Mes = liquidacion.Mes,
            Ordenes = liquidacion.Ordenes,
            Ventas = liquidacion.Ventas,
            Porcentaje = liquidacion.Porcentaje,
            Comision = liquidacion.Comision,
            Estado = liquidacion.Estado,
            FechaCalculo = liquidacion.FechaCalculo,
            FechaPago = liquidacion.FechaPago
        };
    }

    private static (DateTime Inicio, DateTime Fin) LimitesMes(int anio, int mes)
    {
        DateTime inicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        return (inicio, inicio.AddMonths(1));
    }

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        return excepcion.InnerException is Npgsql.PostgresException postgresql
            && postgresql.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation;
    }

    private sealed class ComisionAgrupada
    {
        public int VendedorId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public int Ordenes { get; set; }

        public decimal Ventas { get; set; }

        public decimal Porcentaje { get; set; }
    }
}
