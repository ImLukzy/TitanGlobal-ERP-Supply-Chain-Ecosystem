// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class SiniestroFlota
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public string Lugar { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public DateTime FechaIncidente { get; set; }

    public string? Observaciones { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? NumeroPoliza { get; set; }

    public string? Aseguradora { get; set; }

    public string? NumeroReclamo { get; set; }

    public string? Moneda { get; set; }

    public decimal? MontoReclamado { get; set; }

    public DateTime? FechaReclamo { get; set; }

    public string? Resultado { get; set; }

    public decimal? MontoReconocido { get; set; }

    public string? Resolucion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }
}
