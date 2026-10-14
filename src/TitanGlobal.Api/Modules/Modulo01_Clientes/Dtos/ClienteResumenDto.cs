namespace TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;

public class ClienteResumenDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public int TotalOrdenes { get; set; }
}
