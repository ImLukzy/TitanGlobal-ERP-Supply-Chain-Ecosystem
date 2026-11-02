namespace TitanGlobal.Application.DTOs.Modulo01_Clientes;

public class ClienteMontoDto
{
    public int ClienteId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal MontoTotal { get; set; }

    public int TotalOrdenes { get; set; }
}
