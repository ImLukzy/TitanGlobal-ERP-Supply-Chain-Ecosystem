namespace TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;

public class RegistrarTransferenciaBancariaRequestDto
{
    public int CuentaOrigenId { get; init; }
    public int CuentaDestinoId { get; init; }
    public decimal Importe { get; init; }
    public DateOnly? FechaOperacion { get; init; }
    public string? Referencia { get; init; }
}

public class TransferenciaBancariaDto
{
    public int Id { get; init; }
    public int CuentaOrigenId { get; init; }
    public int CuentaDestinoId { get; init; }
    public string Moneda { get; init; } = string.Empty;
    public decimal Importe { get; init; }
    public DateOnly FechaOperacion { get; init; }
    public int MovimientoEgresoId { get; init; }
    public int MovimientoIngresoId { get; init; }
}
