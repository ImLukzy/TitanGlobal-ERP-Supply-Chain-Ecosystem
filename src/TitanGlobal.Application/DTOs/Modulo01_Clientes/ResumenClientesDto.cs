namespace TitanGlobal.Application.DTOs.Modulo01_Clientes;

public class ResumenClientesDto
{
    public int TotalClientes { get; set; }

    public int ClientesConOrdenes { get; set; }

    public int ClientesSinDescripcion { get; set; }

    public List<ClienteMontoDto> TopClientesPorMonto { get; set; } = new List<ClienteMontoDto>();
}
