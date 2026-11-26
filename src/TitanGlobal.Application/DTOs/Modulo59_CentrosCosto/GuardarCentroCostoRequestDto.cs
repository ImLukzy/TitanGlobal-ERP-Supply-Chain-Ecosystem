namespace TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;

public class GuardarCentroCostoRequestDto
{
    public string? Codigo { get; init; }
    public string? Nombre { get; init; }
    public string? Descripcion { get; init; }
    public int? ResponsableEmpleadoId { get; init; }
    public int? CentroCostoPadreId { get; init; }
}
