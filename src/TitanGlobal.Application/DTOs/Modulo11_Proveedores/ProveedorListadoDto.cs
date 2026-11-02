namespace TitanGlobal.Application.DTOs.Modulo11_Proveedores;

public class ProveedorListadoDto
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
