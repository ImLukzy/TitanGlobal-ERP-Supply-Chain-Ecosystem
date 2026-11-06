// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class VehiculoFlota
{
    public int Id { get; set; }

    public string Placa { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int Anio { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public decimal CapacidadKg { get; set; }

    public bool Activo { get; set; }

    public string? Observaciones { get; set; }
}
