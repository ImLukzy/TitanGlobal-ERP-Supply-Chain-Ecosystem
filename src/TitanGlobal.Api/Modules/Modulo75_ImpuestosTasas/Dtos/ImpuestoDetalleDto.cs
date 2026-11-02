namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;

public class ImpuestoDetalleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Pais { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public List<TasaImpuestoListadoDto> Tasas { get; set; } = new();
}
