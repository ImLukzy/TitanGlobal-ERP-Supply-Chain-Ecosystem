namespace TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;

public class ResumenClientesDto
{
    public int TotalClientes { get; set; }

    public int ClientesConOrdenes { get; set; }

    public int ClientesSinDescripcion { get; set; }

    public List<ClienteMontoDto> TopClientesPorMonto { get; set; } = new List<ClienteMontoDto>();
}
