// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Sucursal
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string Ciudad { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public string? Contacto { get; set; }

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? FechaApertura { get; set; }

    public DateTime FechaRegistro { get; set; }

    public bool Activa { get; set; }

    public DateTime? FechaCambioEstado { get; set; }
}
