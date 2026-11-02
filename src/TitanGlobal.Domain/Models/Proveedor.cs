namespace TitanGlobal.Domain.Models;

public partial class Proveedor
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; } = true;

    public virtual ICollection<SolicitudCompra> SolicitudesCompra { get; set; } = new List<SolicitudCompra>();

    public virtual ICollection<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = new List<OrdenAbastecimiento>();
}
