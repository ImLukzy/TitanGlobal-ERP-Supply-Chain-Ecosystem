namespace TitanGlobal.Application.DTOs.Modulo01_Clientes;

public class ClienteResumenDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public int TotalOrdenes { get; set; }
}
