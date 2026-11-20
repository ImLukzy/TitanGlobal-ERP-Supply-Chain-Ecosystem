namespace TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;

public class TransferenciaDetalleDto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int AlmacenOrigenId { get; set; }

    public string AlmacenOrigenCodigo { get; set; } = string.Empty;

    public int AlmacenDestinoId { get; set; }

    public string AlmacenDestinoCodigo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal ValorTotal { get; set; }

    public DateTime Fecha { get; set; }

    public string? Observaciones { get; set; }

    public int MovimientoSalidaId { get; set; }

    public int MovimientoEntradaId { get; set; }
}
