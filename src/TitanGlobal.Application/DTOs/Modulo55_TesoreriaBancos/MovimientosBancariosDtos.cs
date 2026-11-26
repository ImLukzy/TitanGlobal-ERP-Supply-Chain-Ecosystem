namespace TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;

public class RegistrarMovimientoBancarioRequestDto
{
    public string? Tipo { get; init; }
    public decimal Importe { get; init; }
    public DateOnly? FechaOperacion { get; init; }
    public string? Referencia { get; init; }
}

public class RevertirMovimientoBancarioRequestDto
{
    public string? Motivo { get; init; }
}

public class MovimientoBancarioDto
{
    public int Id { get; init; }
    public int CuentaBancariaId { get; init; }
    public string Moneda { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public decimal Importe { get; init; }
    public DateOnly FechaOperacion { get; init; }
    public string? Referencia { get; init; }
    public int? TransferenciaBancariaId { get; init; }
    public DateTime? FechaReversion { get; init; }
    public string? MotivoReversion { get; init; }
}

public class PaginaMovimientosBancariosDto
{
    public IReadOnlyList<MovimientoBancarioDto> Items { get; init; } = Array.Empty<MovimientoBancarioDto>();
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas { get; init; }
}
