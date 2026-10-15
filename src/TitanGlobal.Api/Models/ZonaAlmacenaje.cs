namespace TitanGlobal.Api.Models;

public partial class ZonaAlmacenaje
{
    public int Id { get; set; }

    public int AlmacenId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Temperatura { get; set; } = string.Empty;

    public decimal CapacidadM3 { get; set; }

    public bool Activa { get; set; } = true;

    public virtual Almacen Almacen { get; set; } = null!;
}
