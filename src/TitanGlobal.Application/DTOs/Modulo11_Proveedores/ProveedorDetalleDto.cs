namespace TitanGlobal.Application.DTOs.Modulo11_Proveedores;

public class ProveedorDetalleDto
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; }
}
