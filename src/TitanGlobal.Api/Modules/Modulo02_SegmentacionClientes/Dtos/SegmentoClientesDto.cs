namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;

public class SegmentoClientesDto
{
    public int SegmentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Clientes { get; set; }
    public decimal MontoTotal { get; set; }
}
