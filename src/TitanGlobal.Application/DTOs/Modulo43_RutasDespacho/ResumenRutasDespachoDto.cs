namespace TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;

public class ResumenRutasDespachoDto
{
    public int TotalRutas { get; set; }

    public int Activas { get; set; }

    public int Inactivas { get; set; }

    public List<ZonaRutasDespachoDto> PorZona { get; set; } = new List<ZonaRutasDespachoDto>();
}
