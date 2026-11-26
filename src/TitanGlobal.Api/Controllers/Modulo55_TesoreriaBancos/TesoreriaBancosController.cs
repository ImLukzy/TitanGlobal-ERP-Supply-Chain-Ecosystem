using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

namespace TitanGlobal.Api.Controllers.Modulo55_TesoreriaBancos;

[ApiController]
[Authorize]
[Route("api/v1/tesoreria-bancos")]
public class TesoreriaBancosController : ControllerBase
{
    private readonly ListarCuentasBancariasUseCase _listarCuentas;
    private readonly CrearCuentaBancariaUseCase _crearCuenta;
    private readonly ObtenerCuentaBancariaUseCase _obtenerCuenta;
    private readonly ActualizarCuentaBancariaUseCase _actualizarCuenta;
    private readonly ListarMovimientosBancariosUseCase _listarMovimientos;
    private readonly RegistrarMovimientoBancarioUseCase _registrarMovimiento;
    private readonly RevertirMovimientoBancarioUseCase _revertirMovimiento;
    private readonly RegistrarTransferenciaBancariaUseCase _registrarTransferencia;
    private readonly ObtenerTransferenciaBancariaUseCase _obtenerTransferencia;

    public TesoreriaBancosController(
        ListarCuentasBancariasUseCase listarCuentas,
        CrearCuentaBancariaUseCase crearCuenta,
        ObtenerCuentaBancariaUseCase obtenerCuenta,
        ActualizarCuentaBancariaUseCase actualizarCuenta,
        ListarMovimientosBancariosUseCase listarMovimientos,
        RegistrarMovimientoBancarioUseCase registrarMovimiento,
        RevertirMovimientoBancarioUseCase revertirMovimiento,
        RegistrarTransferenciaBancariaUseCase registrarTransferencia,
        ObtenerTransferenciaBancariaUseCase obtenerTransferencia)
    {
        _listarCuentas = listarCuentas;
        _crearCuenta = crearCuenta;
        _obtenerCuenta = obtenerCuenta;
        _actualizarCuenta = actualizarCuenta;
        _listarMovimientos = listarMovimientos;
        _registrarMovimiento = registrarMovimiento;
        _revertirMovimiento = revertirMovimiento;
        _registrarTransferencia = registrarTransferencia;
        _obtenerTransferencia = obtenerTransferencia;
    }

    [HttpGet("cuentas")]
    public async Task<ActionResult<PaginaCuentasBancariasDto>> ListarCuentas(
        [FromQuery] string? moneda, [FromQuery] string? tipo, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listarCuentas.ExecuteAsync(moneda, tipo, pagina, tamanoPagina));
    }

    [HttpPost("cuentas")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CuentaBancariaDetalleDto>> CrearCuenta([FromBody] GuardarCuentaBancariaRequestDto solicitud)
    {
        CuentaBancariaDetalleDto cuenta = await _crearCuenta.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(ObtenerCuenta), new { id = cuenta.Id }, cuenta);
    }

    [HttpGet("cuentas/{id:int}")]
    public async Task<ActionResult<CuentaBancariaDetalleDto>> ObtenerCuenta(int id)
    {
        return Ok(await _obtenerCuenta.ExecuteAsync(id));
    }

    [HttpPut("cuentas/{id:int}")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    public async Task<ActionResult<CuentaBancariaDetalleDto>> ActualizarCuenta(int id, [FromBody] GuardarCuentaBancariaRequestDto solicitud)
    {
        return Ok(await _actualizarCuenta.ExecuteAsync(id, solicitud));
    }

    [HttpGet("cuentas/{id:int}/movimientos")]
    public async Task<ActionResult<PaginaMovimientosBancariosDto>> ListarMovimientos(
        int id, [FromQuery] string? tipo, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listarMovimientos.ExecuteAsync(id, tipo, desde, hasta, pagina, tamanoPagina));
    }

    [HttpPost("cuentas/{id:int}/movimientos")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<MovimientoBancarioDto>> RegistrarMovimiento(int id, [FromBody] RegistrarMovimientoBancarioRequestDto solicitud)
    {
        return StatusCode(StatusCodes.Status201Created, await _registrarMovimiento.ExecuteAsync(id, solicitud));
    }

    [HttpPost("cuentas/{id:int}/movimientos/{movimientoId:int}/revertir")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    public async Task<ActionResult<MovimientoBancarioDto>> RevertirMovimiento(
        int id, int movimientoId, [FromBody] RevertirMovimientoBancarioRequestDto solicitud)
    {
        return Ok(await _revertirMovimiento.ExecuteAsync(id, movimientoId, solicitud));
    }

    [HttpPost("transferencias")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TransferenciaBancariaDto>> RegistrarTransferencia([FromBody] RegistrarTransferenciaBancariaRequestDto solicitud)
    {
        TransferenciaBancariaDto transferencia = await _registrarTransferencia.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(ObtenerTransferencia), new { id = transferencia.Id }, transferencia);
    }

    [HttpGet("transferencias/{id:int}")]
    public async Task<ActionResult<TransferenciaBancariaDto>> ObtenerTransferencia(int id)
    {
        return Ok(await _obtenerTransferencia.ExecuteAsync(id));
    }
}
