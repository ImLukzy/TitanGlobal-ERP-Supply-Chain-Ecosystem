namespace TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;

public class SegmentoClientesDto
{
    public int SegmentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Clientes { get; set; }
    public decimal MontoTotal { get; set; }
}
