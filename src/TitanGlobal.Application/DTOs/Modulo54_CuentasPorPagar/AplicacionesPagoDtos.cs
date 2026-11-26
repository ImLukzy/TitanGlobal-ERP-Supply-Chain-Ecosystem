namespace TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;

public class RegistrarAplicacionPagoRequestDto
{
    public decimal Importe { get; init; }
    public DateOnly? FechaPago { get; init; }
}

public class RevertirAplicacionPagoRequestDto
{
    public string? Motivo { get; init; }
}

public class AplicacionPagoDto
{
    public int Id { get; init; }
    public int CuentaPorPagarId { get; init; }
    public decimal Importe { get; init; }
    public string Moneda { get; init; } = string.Empty;
    public DateOnly FechaPago { get; init; }
    public DateTime? FechaReversion { get; init; }
    public string? MotivoReversion { get; init; }
}

public class PaginaAplicacionesPagoDto
{
    public IReadOnlyList<AplicacionPagoDto> Items { get; init; } = Array.Empty<AplicacionPagoDto>();
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas { get; init; }
}
