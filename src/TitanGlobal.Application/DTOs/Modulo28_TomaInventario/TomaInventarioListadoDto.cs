namespace TitanGlobal.Application.DTOs.Modulo28_TomaInventario;

public class TomaInventarioListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public string AlmacenNombre { get; set; } = string.Empty;

    public DateTime FechaToma { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string Responsable { get; set; } = string.Empty;

    public int CantidadProductos { get; set; }

    public int LineasConDiferencia { get; set; }
}
