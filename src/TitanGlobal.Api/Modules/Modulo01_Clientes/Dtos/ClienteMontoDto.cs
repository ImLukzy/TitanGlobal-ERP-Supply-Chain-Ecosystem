namespace TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;

public class ClienteMontoDto
{
    public int ClienteId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal MontoTotal { get; set; }

    public int TotalOrdenes { get; set; }
}
