using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;
using TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.UseCases;

public class PromocionesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public PromocionesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaPromocionesDto> ListarAsync(
        string? busqueda,
        string? tipo,
        int? segmentoId,
        bool? vigentes,
        int pagina,
        int tamanoPagina)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        string? tipoNormalizado = NormalizarTipoFiltro(tipo);
        IQueryable<Promocion> consulta = _unitOfWork.Repository<Promocion>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(promocion => promocion.Codigo.ToLower().Contains(termino)
                || promocion.Nombre.ToLower().Contains(termino));
        }

        if (tipoNormalizado is not null)
        {
            consulta = consulta.Where(promocion => promocion.Tipo == tipoNormalizado);
        }

        if (segmentoId.HasValue)
        {
            consulta = consulta.Where(promocion => promocion.SegmentoClienteId == segmentoId.Value);
        }

        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        if (vigentes == true)
        {
            consulta = consulta.Where(promocion => promocion.Activa
                && promocion.FechaInicio <= hoy && promocion.FechaFin >= hoy);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<Promocion> promociones = await consulta.OrderByDescending(promocion => promocion.FechaInicio)
            .ThenBy(promocion => promocion.Codigo).Skip(saltar).Take(tamanoPagina).ToListAsync();
        List<PromocionListadoDto> items = await ProyectarListadoAsync(promociones, hoy);
        return new PaginaPromocionesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<PromocionDetalleDto> ObtenerAsync(int id)
    {
        Promocion? promocion = await _unitOfWork.Repository<Promocion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (promocion is null)
        {
            throw Error(StatusCodes.Status404NotFound, $"La promoción {id} no existe.");
        }

        List<PromocionListadoDto> resultado = await ProyectarListadoAsync(
            new List<Promocion> { promocion }, FechasUtc.Normalizar(DateTime.UtcNow).Date);
        return CopiarDetalle(resultado[0]);
    }

    public async Task<int> GuardarAsync(GuardarPromocionRequestDto solicitud, int? id)
    {
        await ValidacionPromocion.ValidarAsync(solicitud, id, _unitOfWork);
        Promocion? promocion = null;
        if (id.HasValue)
        {
            promocion = await _unitOfWork.Repository<Promocion>().AsQueryable()
                .SingleOrDefaultAsync(entidad => entidad.Id == id.Value);
            if (promocion is null)
            {
                throw Error(StatusCodes.Status404NotFound, $"La promoción {id.Value} no existe.");
            }
        }

        if (promocion is null)
        {
            promocion = new Promocion();
            await _unitOfWork.Repository<Promocion>().AddAsync(promocion);
        }

        promocion.Codigo = solicitud.Codigo;
        promocion.Nombre = solicitud.Nombre;
        promocion.Tipo = solicitud.Tipo;
        promocion.ProductoId = solicitud.ProductoId;
        promocion.MontoMinimo = solicitud.MontoMinimo;
        promocion.PorcentajeDescuento = solicitud.PorcentajeDescuento;
        promocion.SegmentoClienteId = solicitud.SegmentoClienteId;
        promocion.FechaInicio = FechasUtc.Normalizar(solicitud.FechaInicio);
        promocion.FechaFin = FechasUtc.Normalizar(solicitud.FechaFin);
        promocion.Activa = solicitud.Activa;
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (excepcion.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw Error(StatusCodes.Status409Conflict, $"Ya existe una promoción con el código {solicitud.Codigo}.");
        }

        return promocion.Id;
    }

    public async Task EliminarAsync(int id)
    {
        Promocion? promocion = await _unitOfWork.Repository<Promocion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (promocion is null)
        {
            throw Error(StatusCodes.Status404NotFound, $"La promoción {id} no existe.");
        }

        _unitOfWork.Repository<Promocion>().Remove(promocion);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<EvaluacionCompraDto> EvaluarAsync(EvaluarCompraRequestDto solicitud)
    {
        ClienteClasificadoDto? cliente = await ConsultaSegmentacionClientes.ObtenerClienteClasificadoAsync(
            solicitud.ClienteId, _unitOfWork);
        if (cliente is null)
        {
            throw Error(StatusCodes.Status400BadRequest, $"El cliente {solicitud.ClienteId} no existe.");
        }

        if (solicitud.Lineas is null || solicitud.Lineas.Count == 0)
        {
            throw Error(StatusCodes.Status400BadRequest, "La compra debe tener al menos una línea.");
        }

        foreach (LineaCompraRequestDto linea in solicitud.Lineas)
        {
            if (linea.Cantidad <= 0)
            {
                throw Error(StatusCodes.Status400BadRequest, "La cantidad debe ser mayor que 0.");
            }

            if (linea.PrecioUnitario <= 0m)
            {
                throw Error(StatusCodes.Status400BadRequest, "El precio unitario debe ser mayor que 0.");
            }
        }

        List<int> idsProductos = solicitud.Lineas.Select(linea => linea.ProductoId).Distinct().ToList();
        List<Producto> productos = await _unitOfWork.Repository<Producto>().AsQueryable()
            .Where(producto => idsProductos.Contains(producto.Id)).ToListAsync();
        Dictionary<int, Producto> productosPorId = productos.ToDictionary(producto => producto.Id);
        foreach (LineaCompraRequestDto linea in solicitud.Lineas)
        {
            if (!productosPorId.ContainsKey(linea.ProductoId))
            {
                throw Error(StatusCodes.Status400BadRequest, $"El producto {linea.ProductoId} no existe.");
            }
        }

        DateTime fecha = FechasUtc.Normalizar(solicitud.Fecha ?? DateTime.UtcNow).Date;
        List<Promocion> promociones = await _unitOfWork.Repository<Promocion>().AsQueryable()
            .Where(promocion => promocion.Activa
                && promocion.FechaInicio <= fecha
                && promocion.FechaFin >= fecha
                && (!promocion.SegmentoClienteId.HasValue || promocion.SegmentoClienteId == cliente.SegmentoId)
                && (promocion.Tipo == "MontoMinimo"
                    || (promocion.Tipo == "Producto" && promocion.ProductoId.HasValue
                        && idsProductos.Contains(promocion.ProductoId.Value))))
            .ToListAsync();

        List<LineaEvaluadaDto> lineasEvaluadas = new List<LineaEvaluadaDto>();
        foreach (LineaCompraRequestDto linea in solicitud.Lineas)
        {
            Producto producto = productosPorId[linea.ProductoId];
            Promocion? promocionLinea = promociones
                .Where(promocion => promocion.Tipo == "Producto" && promocion.ProductoId == linea.ProductoId)
                .OrderByDescending(promocion => promocion.PorcentajeDescuento)
                .ThenBy(promocion => promocion.Id)
                .FirstOrDefault();
            decimal subtotal = linea.Cantidad * linea.PrecioUnitario;
            decimal descuento = promocionLinea is null
                ? 0m
                : decimal.Round(subtotal * promocionLinea.PorcentajeDescuento / 100m, 2, MidpointRounding.AwayFromZero);
            lineasEvaluadas.Add(new LineaEvaluadaDto
            {
                ProductoId = linea.ProductoId,
                ProductoNombre = producto.Nombre,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                Subtotal = subtotal,
                Promocion = promocionLinea is null ? null : CrearPromocionAplicada(promocionLinea),
                Descuento = descuento,
                Importe = subtotal - descuento
            });
        }

        decimal totalTrasLineas = lineasEvaluadas.Sum(linea => linea.Importe);
        Promocion? promocionCompra = promociones
            .Where(promocion => promocion.Tipo == "MontoMinimo" && promocion.MontoMinimo <= totalTrasLineas)
            .OrderByDescending(promocion => promocion.PorcentajeDescuento)
            .ThenBy(promocion => promocion.Id)
            .FirstOrDefault();
        decimal descuentoCompra = promocionCompra is null
            ? 0m
            : decimal.Round(totalTrasLineas * promocionCompra.PorcentajeDescuento / 100m, 2, MidpointRounding.AwayFromZero);
        decimal subtotalCompra = lineasEvaluadas.Sum(linea => linea.Subtotal);
        decimal descuentoLineas = lineasEvaluadas.Sum(linea => linea.Descuento);
        return new EvaluacionCompraDto
        {
            ClienteId = cliente.ClienteId,
            ClienteNombre = cliente.Nombre,
            SegmentoCodigo = cliente.SegmentoCodigo,
            Fecha = fecha,
            Lineas = lineasEvaluadas,
            TotalTrasLineas = totalTrasLineas,
            PromocionCompra = promocionCompra is null ? null : CrearPromocionAplicada(promocionCompra),
            DescuentoCompra = descuentoCompra,
            Subtotal = subtotalCompra,
            DescuentoTotal = descuentoLineas + descuentoCompra,
            Total = totalTrasLineas - descuentoCompra
        };
    }

    public async Task<ResumenPromocionesDto> ResumenAsync()
    {
        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        IQueryable<Promocion> consulta = _unitOfWork.Repository<Promocion>().AsQueryable();
        int total = await consulta.CountAsync();
        int activas = await consulta.CountAsync(promocion => promocion.Activa);
        List<Promocion> vigentes = await consulta.Where(promocion => promocion.Activa
            && promocion.FechaInicio <= hoy && promocion.FechaFin >= hoy).ToListAsync();
        decimal promedio = vigentes.Count == 0
            ? 0m
            : decimal.Round(vigentes.Average(promocion => promocion.PorcentajeDescuento), 2, MidpointRounding.AwayFromZero);
        return new ResumenPromocionesDto
        {
            Total = total,
            Activas = activas,
            Vigentes = vigentes.Count,
            DeProducto = await consulta.CountAsync(promocion => promocion.Tipo == "Producto"),
            DeMontoMinimo = await consulta.CountAsync(promocion => promocion.Tipo == "MontoMinimo"),
            DeSegmento = await consulta.CountAsync(promocion => promocion.SegmentoClienteId.HasValue),
            PorcentajePromedioVigentes = promedio
        };
    }

    private async Task<List<PromocionListadoDto>> ProyectarListadoAsync(List<Promocion> promociones, DateTime hoy)
    {
        List<int> productosIds = promociones.Where(promocion => promocion.ProductoId.HasValue)
            .Select(promocion => promocion.ProductoId!.Value).Distinct().ToList();
        List<Producto> productos = await _unitOfWork.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id)).ToListAsync();
        Dictionary<int, string> nombresProductos = productos.ToDictionary(producto => producto.Id, producto => producto.Nombre);
        List<int> segmentosIds = promociones.Where(promocion => promocion.SegmentoClienteId.HasValue)
            .Select(promocion => promocion.SegmentoClienteId!.Value).Distinct().ToList();
        List<SegmentoCliente> segmentos = await _unitOfWork.Repository<SegmentoCliente>().AsQueryable()
            .Where(segmento => segmentosIds.Contains(segmento.Id)).ToListAsync();
        Dictionary<int, string> codigosSegmentos = segmentos.ToDictionary(segmento => segmento.Id, segmento => segmento.Codigo);
        List<PromocionListadoDto> resultado = new List<PromocionListadoDto>();
        foreach (Promocion promocion in promociones)
        {
            resultado.Add(new PromocionListadoDto
            {
                Id = promocion.Id,
                Codigo = promocion.Codigo,
                Nombre = promocion.Nombre,
                Tipo = promocion.Tipo,
                ProductoId = promocion.ProductoId,
                ProductoNombre = promocion.ProductoId.HasValue
                    ? nombresProductos.GetValueOrDefault(promocion.ProductoId.Value)
                    : null,
                MontoMinimo = promocion.MontoMinimo,
                PorcentajeDescuento = promocion.PorcentajeDescuento,
                SegmentoId = promocion.SegmentoClienteId,
                SegmentoCodigo = promocion.SegmentoClienteId.HasValue
                    ? codigosSegmentos.GetValueOrDefault(promocion.SegmentoClienteId.Value)
                    : null,
                FechaInicio = promocion.FechaInicio,
                FechaFin = promocion.FechaFin,
                Activa = promocion.Activa,
                Vigente = promocion.Activa && promocion.FechaInicio <= hoy && promocion.FechaFin >= hoy
            });
        }

        return resultado;
    }

    private static PromocionDetalleDto CopiarDetalle(PromocionListadoDto promocion)
    {
        return new PromocionDetalleDto
        {
            Id = promocion.Id,
            Codigo = promocion.Codigo,
            Nombre = promocion.Nombre,
            Tipo = promocion.Tipo,
            ProductoId = promocion.ProductoId,
            ProductoNombre = promocion.ProductoNombre,
            MontoMinimo = promocion.MontoMinimo,
            PorcentajeDescuento = promocion.PorcentajeDescuento,
            SegmentoId = promocion.SegmentoId,
            SegmentoCodigo = promocion.SegmentoCodigo,
            FechaInicio = promocion.FechaInicio,
            FechaFin = promocion.FechaFin,
            Activa = promocion.Activa,
            Vigente = promocion.Vigente
        };
    }

    private static PromocionAplicadaDto CrearPromocionAplicada(Promocion promocion)
    {
        return new PromocionAplicadaDto
        {
            Id = promocion.Id,
            Codigo = promocion.Codigo,
            Nombre = promocion.Nombre,
            PorcentajeDescuento = promocion.PorcentajeDescuento
        };
    }

    private static string? NormalizarTipoFiltro(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return null;
        }

        string normalizado = tipo.Trim();
        if (normalizado.Equals("Producto", StringComparison.OrdinalIgnoreCase))
        {
            return "Producto";
        }

        if (normalizado.Equals("MontoMinimo", StringComparison.OrdinalIgnoreCase))
        {
            return "MontoMinimo";
        }

        throw Error(StatusCodes.Status400BadRequest, "El tipo de promoción no es válido.");
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw Error(StatusCodes.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Error(StatusCodes.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }
    }

    private static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }
}
