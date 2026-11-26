namespace TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;

public class RegistrarCuentaPorPagarRequestDto
{
    public int ProveedorId { get; init; }
    public int? OrdenAbastecimientoId { get; init; }
    public string? Serie { get; init; }
    public string? NumeroDocumento { get; init; }
    public DateOnly? FechaDocumento { get; init; }
    public DateOnly? FechaVencimiento { get; init; }
    public string? Moneda { get; init; }
    public decimal? BaseImponible { get; init; }
    public decimal? ImporteImpuesto { get; init; }
}

public class AnularCuentaPorPagarRequestDto
{
    public string? Motivo { get; init; }
}

public class CuentaPorPagarListadoDto
{
    public int Id { get; init; }
    public int ProveedorId { get; init; }
    public string Serie { get; init; } = string.Empty;
    public string NumeroDocumento { get; init; } = string.Empty;
    public string Moneda { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public decimal Saldo { get; init; }
    public DateOnly FechaVencimiento { get; init; }
    public bool Anulada { get; init; }
    public bool Vencida { get; init; }
}

public class CuentaPorPagarDetalleDto : CuentaPorPagarListadoDto
{
    public int? OrdenAbastecimientoId { get; init; }
    public DateOnly FechaDocumento { get; init; }
    public decimal BaseImponible { get; init; }
    public decimal ImporteImpuesto { get; init; }
    public decimal ImportePagado { get; init; }
    public DateTime? FechaAnulacion { get; init; }
    public string? MotivoAnulacion { get; init; }
}

public class PaginaCuentasPorPagarDto
{
    public IReadOnlyList<CuentaPorPagarListadoDto> Items { get; init; } = Array.Empty<CuentaPorPagarListadoDto>();
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas { get; init; }
}

public class ResumenCuentasPorPagarDto
{
    public DateOnly FechaCorte { get; init; }
    public IReadOnlyList<ResumenProveedorMonedaDto> Grupos { get; init; } = Array.Empty<ResumenProveedorMonedaDto>();
}

public class ResumenProveedorMonedaDto
{
    public int ProveedorId { get; init; }
    public string Moneda { get; init; } = string.Empty;
    public decimal SaldoAlDia { get; init; }
    public decimal SaldoVencido { get; init; }
    public decimal Vencido0A30 { get; init; }
    public decimal Vencido31A60 { get; init; }
    public decimal Vencido61A90 { get; init; }
    public decimal VencidoMasDe90 { get; init; }
}
