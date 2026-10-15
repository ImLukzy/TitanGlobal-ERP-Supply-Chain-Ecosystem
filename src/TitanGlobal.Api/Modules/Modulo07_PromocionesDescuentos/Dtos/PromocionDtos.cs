namespace TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.Dtos;

public class PaginaPromocionesDto
{
    public List<PromocionListadoDto> Items { get; set; } = new List<PromocionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class PromocionListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public int? ProductoId { get; set; }

    public string? ProductoNombre { get; set; }

    public decimal? MontoMinimo { get; set; }

    public decimal PorcentajeDescuento { get; set; }

    public int? SegmentoId { get; set; }

    public string? SegmentoCodigo { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public bool Activa { get; set; }

    public bool Vigente { get; set; }
}

public class PromocionDetalleDto : PromocionListadoDto
{
}

public class EvaluarCompraRequestDto
{
    public int ClienteId { get; set; }

    public DateTime? Fecha { get; set; }

    public List<LineaCompraRequestDto> Lineas { get; set; } = new List<LineaCompraRequestDto>();
}

public class LineaCompraRequestDto
{
    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}

public class EvaluacionCompraDto
{
    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public string? SegmentoCodigo { get; set; }

    public DateTime Fecha { get; set; }

    public List<LineaEvaluadaDto> Lineas { get; set; } = new List<LineaEvaluadaDto>();

    public decimal TotalTrasLineas { get; set; }

    public PromocionAplicadaDto? PromocionCompra { get; set; }

    public decimal DescuentoCompra { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DescuentoTotal { get; set; }

    public decimal Total { get; set; }
}

public class LineaEvaluadaDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public PromocionAplicadaDto? Promocion { get; set; }

    public decimal Descuento { get; set; }

    public decimal Importe { get; set; }
}

public class PromocionAplicadaDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public decimal PorcentajeDescuento { get; set; }
}

public class ResumenPromocionesDto
{
    public int Total { get; set; }

    public int Activas { get; set; }

    public int Vigentes { get; set; }

    public int DeProducto { get; set; }

    public int DeMontoMinimo { get; set; }

    public int DeSegmento { get; set; }

    public decimal PorcentajePromedioVigentes { get; set; }
}

public class PromocionCreadaDto
{
    public int Id { get; set; }
}
