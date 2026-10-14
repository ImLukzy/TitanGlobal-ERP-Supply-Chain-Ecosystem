namespace TitanGlobal.Api.Models;

public partial class Cliente
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public virtual ICollection<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();
}
