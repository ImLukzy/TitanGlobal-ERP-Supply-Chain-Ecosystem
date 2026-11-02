namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;

public class GuardarImpuestoRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Pais { get; set; } = "PER";

    public bool Activo { get; set; } = true;
}
