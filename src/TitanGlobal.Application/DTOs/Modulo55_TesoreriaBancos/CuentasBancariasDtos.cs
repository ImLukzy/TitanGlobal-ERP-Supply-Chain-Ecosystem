namespace TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;

public class GuardarCuentaBancariaRequestDto
{
    public string? Banco { get; init; }
    public string? Numero { get; init; }
    public string? Nombre { get; init; }
    public string? Tipo { get; init; }
    public string? Moneda { get; init; }
}

public class CuentaBancariaListadoDto
{
    public int Id { get; init; }
    public string Banco { get; init; } = string.Empty;
    public string Numero { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Moneda { get; init; } = string.Empty;
    public decimal Saldo { get; init; }
}

public class CuentaBancariaDetalleDto : CuentaBancariaListadoDto
{
    public DateTime FechaRegistro { get; init; }
}

public class PaginaCuentasBancariasDto
{
    public IReadOnlyList<CuentaBancariaListadoDto> Items { get; init; } = Array.Empty<CuentaBancariaListadoDto>();
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
    public int TotalRegistros { get; init; }
    public int TotalPaginas { get; init; }
}
