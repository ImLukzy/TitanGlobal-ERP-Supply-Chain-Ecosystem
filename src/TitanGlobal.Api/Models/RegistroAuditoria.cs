namespace TitanGlobal.Api.Models;

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
