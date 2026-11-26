namespace TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;

public class CentroCostoListadoDto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public int? CentroCostoPadreId { get; init; }
    public int? ResponsableEmpleadoId { get; init; }
    public bool Activo { get; init; }
}

public class CentroCostoDetalleDto : CentroCostoListadoDto
{
    public string? Descripcion { get; init; }
    public int Nivel { get; init; }
    public DateTime FechaRegistro { get; init; }
    public DateTime? FechaDesactivacion { get; init; }
}

public class PaginaCentrosCostoDto
{
    public IReadOnlyList<CentroCostoListadoDto> Items { get; init; } = Array.Empty<CentroCostoListadoDto>();
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas { get; init; }
}
