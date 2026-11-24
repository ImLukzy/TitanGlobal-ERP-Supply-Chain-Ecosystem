namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class ResumenTransportistasTercerosDto
{
    public int TotalTransportistas { get; set; }

    public int Activos { get; set; }

    public int Inactivos { get; set; }

    public List<TipoServicioTransportistasDto> PorTipoServicio { get; set; } = new List<TipoServicioTransportistasDto>();
}
