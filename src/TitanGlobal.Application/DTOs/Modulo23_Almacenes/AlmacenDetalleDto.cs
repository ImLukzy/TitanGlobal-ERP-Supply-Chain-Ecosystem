namespace TitanGlobal.Application.DTOs.Modulo23_Almacenes;

public class AlmacenDetalleDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public string Ciudad { get; set; } = string.Empty;

    public decimal CapacidadM3 { get; set; }

    public string? Responsable { get; set; }

    public bool Activo { get; set; }
}
