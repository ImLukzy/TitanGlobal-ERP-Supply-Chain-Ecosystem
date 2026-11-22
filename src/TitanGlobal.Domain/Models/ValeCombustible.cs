// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ValeCombustible
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public string Estacion { get; set; } = string.Empty;

    public DateTime FechaCarga { get; set; }

    public string TipoCombustible { get; set; } = string.Empty;

    public decimal Litros { get; set; }

    public decimal PrecioLitro { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public int OdometroInicial { get; set; }

    public int? OdometroFinal { get; set; }

    public string? Observaciones { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaConfirmacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }
}
