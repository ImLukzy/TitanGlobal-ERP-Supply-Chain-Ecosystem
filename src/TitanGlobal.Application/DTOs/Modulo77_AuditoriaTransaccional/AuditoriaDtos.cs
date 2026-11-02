namespace TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;

public class PaginaAuditoriaDto
{
    public List<RegistroListadoDto> Items { get; set; } = new List<RegistroListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class RegistroListadoDto
{
    public long Id { get; set; }

    public DateTime Fecha { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public string EntidadId { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string? Ruta { get; set; }

    public int CamposCambiados { get; set; }
}

public class RegistroDetalleDto : RegistroListadoDto
{
    public List<CambioCampoDto> Cambios { get; set; } = new List<CambioCampoDto>();
}

public class CambioCampoDto
{
    public string Campo { get; set; } = string.Empty;

    public string? Antes { get; set; }

    public string? Despues { get; set; }
}

public class ResumenAuditoriaDto
{
    public int Total { get; set; }

    public int Altas { get; set; }

    public int Modificaciones { get; set; }

    public int Bajas { get; set; }

    public List<EntidadAuditoriaDto> PorEntidad { get; set; } = new List<EntidadAuditoriaDto>();

    public List<DiaAuditoriaDto> Ultimos7Dias { get; set; } = new List<DiaAuditoriaDto>();
}

public class EntidadAuditoriaDto
{
    public string Entidad { get; set; } = string.Empty;

    public int Registros { get; set; }
}

public class DiaAuditoriaDto
{
    public DateTime Fecha { get; set; }

    public int Registros { get; set; }
}
