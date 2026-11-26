// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class RutaDespacho
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Zona { get; set; } = string.Empty;

    public string Origen { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal DistanciaKm { get; set; }

    public decimal PeajeEstimado { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public string? Observaciones { get; set; }
}
