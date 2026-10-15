namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;

public class ClienteClasificadoDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public int TotalOrdenes { get; set; }
    public decimal MontoTotal { get; set; }
    public int? SegmentoId { get; set; }
    public string? SegmentoCodigo { get; set; }
    public string? SegmentoNombre { get; set; }
}
