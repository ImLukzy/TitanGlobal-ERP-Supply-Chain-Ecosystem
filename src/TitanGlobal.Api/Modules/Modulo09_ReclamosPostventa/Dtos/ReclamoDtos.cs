namespace TitanGlobal.Api.Modules.Modulo09_ReclamosPostventa.Dtos;

public class PaginaReclamosDto
{
    public List<ReclamoListadoDto> Items { get; set; } = new List<ReclamoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ReclamoListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public int OrdenCompraId { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    public string? ProductoNombre { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Prioridad { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaLimite { get; set; }

    public bool Vencido { get; set; }
}

public class ReclamoDetalleDto : ReclamoListadoDto
{
    public string Descripcion { get; set; } = string.Empty;

    public string? Resolucion { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public bool? AtendidoATiempo { get; set; }
}

public class ResumenReclamosDto
{
    public int Total { get; set; }

    public int Abiertos { get; set; }

    public int EnProceso { get; set; }

    public int Resueltos { get; set; }

    public int Cerrados { get; set; }

    public int Rechazados { get; set; }

    public int Vencidos { get; set; }

    public List<TipoReclamosDto> PorTipo { get; set; } = new List<TipoReclamosDto>();

    public decimal HorasPromedioResolucion { get; set; }

    public decimal CumplimientoPlazo { get; set; }
}

public class TipoReclamosDto
{
    public string Tipo { get; set; } = string.Empty;

    public int Reclamos { get; set; }
}

public class ReclamoCreadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;
}
