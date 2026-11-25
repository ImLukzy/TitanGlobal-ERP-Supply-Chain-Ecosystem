using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

namespace TitanGlobal.Api.Controllers.Modulo54_CuentasPorPagar;

[ApiController]
[Authorize]
[Route("api/v1/cuentas-por-pagar")]
public class CuentasPorPagarController : ControllerBase
{
    private readonly ListarCuentasPorPagarUseCase _listar;
    private readonly ObtenerCuentaPorPagarUseCase _obtener;
    private readonly ResumirCuentasPorPagarUseCase _resumir;
    private readonly ListarAplicacionesPagoUseCase _listarPagos;
    private readonly RegistrarCuentaPorPagarUseCase _registrar;
    private readonly RegistrarAplicacionPagoUseCase _registrarPago;
    private readonly RevertirAplicacionPagoUseCase _revertirPago;
    private readonly AnularCuentaPorPagarUseCase _anular;

    public CuentasPorPagarController(
        ListarCuentasPorPagarUseCase listar,
        ObtenerCuentaPorPagarUseCase obtener,
        ResumirCuentasPorPagarUseCase resumir,
        ListarAplicacionesPagoUseCase listarPagos,
        RegistrarCuentaPorPagarUseCase registrar,
        RegistrarAplicacionPagoUseCase registrarPago,
        RevertirAplicacionPagoUseCase revertirPago,
        AnularCuentaPorPagarUseCase anular)
    {
        _listar = listar;
        _obtener = obtener;
        _resumir = resumir;
        _listarPagos = listarPagos;
        _registrar = registrar;
        _registrarPago = registrarPago;
        _revertirPago = revertirPago;
        _anular = anular;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaCuentasPorPagarDto>> Listar(
        [FromQuery] int? proveedorId, [FromQuery] string? moneda, [FromQuery] bool? soloVencidas,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listar.ExecuteAsync(proveedorId, moneda, soloVencidas, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CuentaPorPagarDetalleDto>> Obtener(int id)
    {
        return Ok(await _obtener.ExecuteAsync(id));
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenCuentasPorPagarDto>> Resumir([FromQuery] int? proveedorId, [FromQuery] string? moneda)
    {
        return Ok(await _resumir.ExecuteAsync(proveedorId, moneda));
    }

    [HttpGet("{id:int}/pagos")]
    public async Task<ActionResult<PaginaAplicacionesPagoDto>> ListarPagos(int id, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listarPagos.ExecuteAsync(id, pagina, tamanoPagina));
    }

    [HttpPost]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CuentaPorPagarDetalleDto>> Registrar([FromBody] RegistrarCuentaPorPagarRequestDto solicitud)
    {
        CuentaPorPagarDetalleDto cuenta = await _registrar.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id = cuenta.Id }, cuenta);
    }

    [HttpPost("{id:int}/pagos")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<AplicacionPagoDto>> RegistrarPago(int id, [FromBody] RegistrarAplicacionPagoRequestDto solicitud)
    {
        return StatusCode(StatusCodes.Status201Created, await _registrarPago.ExecuteAsync(id, solicitud));
    }

    [HttpPost("{id:int}/pagos/{pagoId:int}/revertir")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    public async Task<ActionResult<AplicacionPagoDto>> RevertirPago(int id, int pagoId, [FromBody] RevertirAplicacionPagoRequestDto solicitud)
    {
        return Ok(await _revertirPago.ExecuteAsync(id, pagoId, solicitud));
    }

    [HttpPost("{id:int}/anular")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    public async Task<ActionResult<CuentaPorPagarDetalleDto>> Anular(int id, [FromBody] AnularCuentaPorPagarRequestDto solicitud)
    {
        return Ok(await _anular.ExecuteAsync(id, solicitud));
    }
}
