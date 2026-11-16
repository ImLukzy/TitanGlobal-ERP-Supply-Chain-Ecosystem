using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

namespace TitanGlobal.Api.Controllers.Modulo26_KardexValorizado;

[ApiController]
[Route("api/v1/kardex-valorizado")]
public class KardexValorizadoController : ControllerBase
{
    private readonly ListarMovimientosUseCase _listarMovimientosUseCase;

    private readonly RegistrarMovimientoUseCase _registrarMovimientoUseCase;

    private readonly ConsultarKardexUseCase _consultarKardexUseCase;

    private readonly ListarSaldosKardexUseCase _listarSaldosKardexUseCase;

    private readonly ResumenKardexUseCase _resumenKardexUseCase;

    public KardexValorizadoController(
        ListarMovimientosUseCase listarMovimientosUseCase,
        RegistrarMovimientoUseCase registrarMovimientoUseCase,
        ConsultarKardexUseCase consultarKardexUseCase,
        ListarSaldosKardexUseCase listarSaldosKardexUseCase,
        ResumenKardexUseCase resumenKardexUseCase)
    {
        _listarMovimientosUseCase = listarMovimientosUseCase;
        _registrarMovimientoUseCase = registrarMovimientoUseCase;
        _consultarKardexUseCase = consultarKardexUseCase;
        _listarSaldosKardexUseCase = listarSaldosKardexUseCase;
        _resumenKardexUseCase = resumenKardexUseCase;
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> Listar(
        [FromQuery] int? productoId,
        [FromQuery] int? almacenId,
        [FromQuery] string? tipo,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaMovimientosDto resultado = await _listarMovimientosUseCase.ExecuteAsync(
            productoId,
            almacenId,
            tipo,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpPost("movimientos")]
    public async Task<IActionResult> Registrar([FromBody] GuardarMovimientoRequestDto solicitud)
    {
        int id = await _registrarMovimientoUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpGet("kardex")]
    public async Task<IActionResult> Consultar(
        [FromQuery] int productoId,
        [FromQuery] int almacenId,
        [FromQuery] string? metodo)
    {
        KardexDto resultado = await _consultarKardexUseCase.ExecuteAsync(productoId, almacenId, metodo);

        return Ok(resultado);
    }

    [HttpGet("saldos")]
    public async Task<IActionResult> Saldos([FromQuery] int? almacenId)
    {
        List<SaldoKardexDto> resultado = await _listarSaldosKardexUseCase.ExecuteAsync(almacenId);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenKardexDto resultado = await _resumenKardexUseCase.ExecuteAsync();

        return Ok(resultado);
    }
}
