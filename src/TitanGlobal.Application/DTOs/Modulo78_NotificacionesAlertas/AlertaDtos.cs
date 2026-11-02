namespace TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;

public class AlertaDto
{
    public string Clave { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Severidad { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public string Modulo { get; set; } = string.Empty;

    public int RegistroId { get; set; }

    public string Ruta { get; set; } = string.Empty;

    public DateTime FechaReferencia { get; set; }

    public bool Descartada { get; set; }
}

public class PaginaAlertasDto
{
    public List<AlertaDto> Items { get; set; } = new List<AlertaDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ResumenAlertasDto
{
    public int Total { get; set; }

    public int Altas { get; set; }

    public int Medias { get; set; }

    public int Bajas { get; set; }

    public int Descartadas { get; set; }

    public List<TipoAlertasDto> PorTipo { get; set; } = new List<TipoAlertasDto>();
}

public class TipoAlertasDto
{
    public string Tipo { get; set; } = string.Empty;

    public int Alertas { get; set; }
}

public class ClaveAlertaRequestDto
{
    public string Clave { get; set; } = string.Empty;
}
