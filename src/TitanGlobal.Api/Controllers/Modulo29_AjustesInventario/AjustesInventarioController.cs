using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;
using TitanGlobal.Application.UseCases.Modulo29_AjustesInventario;

namespace TitanGlobal.Api.Controllers.Modulo29_AjustesInventario;

[ApiController]
[Route("api/v1/ajustes-inventario")]
public class AjustesInventarioController : ControllerBase
{
    private readonly ListarAjustesInventarioUseCase _listarUseCase;

    private readonly ObtenerAjusteInventarioUseCase _obtenerUseCase;

    private readonly RegistrarAjusteInventarioUseCase _registrarUseCase;

    private readonly ResumenAjustesInventarioUseCase _resumenUseCase;

    private readonly ValoresInicialesAjustesUseCase _valoresInicialesUseCase;

    public AjustesInventarioController(
        ListarAjustesInventarioUseCase listarUseCase,
        ObtenerAjusteInventarioUseCase obtenerUseCase,
        RegistrarAjusteInventarioUseCase registrarUseCase,
        ResumenAjustesInventarioUseCase resumenUseCase,
        ValoresInicialesAjustesUseCase valoresInicialesUseCase)
    {
        _listarUseCase = listarUseCase;
        _obtenerUseCase = obtenerUseCase;
        _registrarUseCase = registrarUseCase;
        _resumenUseCase = resumenUseCase;
        _valoresInicialesUseCase = valoresInicialesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? almacenId,
        [FromQuery] int? productoId,
        [FromQuery] string? tipo,
        [FromQuery] string? motivo,
        [FromQuery] string? busqueda,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAjustesInventarioDto resultado = await _listarUseCase.ExecuteAsync(
            almacenId,
            productoId,
            tipo,
            motivo,
            busqueda,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        AjusteInventarioItemDto resultado = await _obtenerUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarAjusteInventarioRequestDto solicitud)
    {
        AjusteInventarioItemDto creado = await _registrarUseCase.ExecuteAsync(solicitud);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(
        [FromQuery] int? almacenId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        ResumenAjustesInventarioDto resultado = await _resumenUseCase.ExecuteAsync(
            almacenId,
            fechaDesde,
            fechaHasta);

        return Ok(resultado);
    }

    [HttpGet("valores-iniciales")]
    public IActionResult ValoresIniciales()
    {
        ValoresInicialesAjustesDto resultado = _valoresInicialesUseCase.Execute();

        return Ok(resultado);
    }
}
