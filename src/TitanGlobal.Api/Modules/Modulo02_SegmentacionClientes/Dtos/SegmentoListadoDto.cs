namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;

public class SegmentoListadoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Prioridad { get; set; }
    public decimal MontoMinimo { get; set; }
    public int OrdenesMinimas { get; set; }
    public bool Activo { get; set; }
    public int Clientes { get; set; }
}
