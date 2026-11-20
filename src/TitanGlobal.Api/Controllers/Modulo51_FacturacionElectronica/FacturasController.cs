using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

namespace TitanGlobal.Api.Controllers.Modulo51_FacturacionElectronica;

[ApiController]
[Route("api/v1/facturas")]
public class FacturasController : ControllerBase
{
    private readonly CrearFacturaUseCase _crearFacturaUseCase;

    private readonly ActualizarFacturaUseCase _actualizarFacturaUseCase;

    private readonly ObtenerFacturaUseCase _obtenerFacturaUseCase;

    private readonly ListarFacturasUseCase _listarFacturasUseCase;

    private readonly EmitirFacturaUseCase _emitirFacturaUseCase;

    private readonly AnularFacturaUseCase _anularFacturaUseCase;

    private readonly ResumenFacturasUseCase _resumenFacturasUseCase;

    public FacturasController(
        CrearFacturaUseCase crearFacturaUseCase,
        ActualizarFacturaUseCase actualizarFacturaUseCase,
        ObtenerFacturaUseCase obtenerFacturaUseCase,
        ListarFacturasUseCase listarFacturasUseCase,
        EmitirFacturaUseCase emitirFacturaUseCase,
        AnularFacturaUseCase anularFacturaUseCase,
        ResumenFacturasUseCase resumenFacturasUseCase)
    {
        _crearFacturaUseCase = crearFacturaUseCase;

        _actualizarFacturaUseCase = actualizarFacturaUseCase;

        _obtenerFacturaUseCase = obtenerFacturaUseCase;

        _listarFacturasUseCase = listarFacturasUseCase;

        _emitirFacturaUseCase = emitirFacturaUseCase;

        _anularFacturaUseCase = anularFacturaUseCase;

        _resumenFacturasUseCase = resumenFacturasUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda = null,
        [FromQuery] int? clienteId = null,
        [FromQuery] string? tipoComprobante = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? moneda = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaFacturasDto resultado =
            await _listarFacturasUseCase.ExecuteAsync(
                busqueda,
                clienteId,
                tipoComprobante,
                estado,
                moneda,
                fechaDesde,
                fechaHasta,
                pagina,
                tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(
        [FromQuery] string moneda = "PEN",
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null)
    {
        ResumenFacturasDto resultado =
            await _resumenFacturasUseCase.ExecuteAsync(
                moneda,
                fechaDesde,
                fechaHasta);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        FacturaDetalleDto resultado =
            await _obtenerFacturaUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarFacturaRequestDto solicitud)
    {
        FacturaDetalleDto resultado =
            await _crearFacturaUseCase.ExecuteAsync(solicitud);

        return CreatedAtAction(
            nameof(Obtener),
            new { id = resultado.Id },
            resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] GuardarFacturaRequestDto solicitud)
    {
        FacturaDetalleDto resultado =
            await _actualizarFacturaUseCase.ExecuteAsync(
                id,
                solicitud);

        return Ok(resultado);
    }

    [HttpPost("{id:int}/emitir")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Emitir(int id)
    {
        FacturaDetalleDto resultado =
            await _emitirFacturaUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(
        int id,
        [FromBody] AnularFacturaRequestDto solicitud)
    {
        FacturaDetalleDto resultado =
            await _anularFacturaUseCase.ExecuteAsync(
                id,
                solicitud);

        return Ok(resultado);
    }
}
