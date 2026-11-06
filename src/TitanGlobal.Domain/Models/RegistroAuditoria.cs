// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class RegistroAuditoria
{
    public long Id { get; set; }

    public DateTime Fecha { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public string EntidadId { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string? Ruta { get; set; }

    public string Cambios { get; set; } = string.Empty;
}
