namespace TitanGlobal.Api.Modules.Modulo11_Proveedores.Dtos;

public class GuardarProveedorRequestDto
{
    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; } = true;
}
