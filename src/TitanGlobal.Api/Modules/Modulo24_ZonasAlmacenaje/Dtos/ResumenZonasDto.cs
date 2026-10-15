namespace TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.Dtos;

public class ResumenZonasDto
{
    public int TotalZonas { get; set; }

    public int Activas { get; set; }

    public List<TipoZonaDto> PorTipo { get; set; } = new List<TipoZonaDto>();

    public List<OcupacionAlmacenDto> Ocupacion { get; set; } = new List<OcupacionAlmacenDto>();
}
