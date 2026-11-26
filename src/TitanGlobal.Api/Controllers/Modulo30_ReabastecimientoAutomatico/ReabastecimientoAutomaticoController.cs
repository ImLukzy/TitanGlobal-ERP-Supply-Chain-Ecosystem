using TitanGlobal.Api.Middlewares;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

namespace TitanGlobal.Api.Controllers.Modulo30_ReabastecimientoAutomatico;

[ApiController]
[Route("api/v1/reabastecimiento-automatico")]
public class ReabastecimientoAutomaticoController : ControllerBase
{
    private readonly ListarReglasReabastecimientoUseCase _listarReglasUseCase;

    private readonly ObtenerReglaReabastecimientoUseCase _obtenerReglaUseCase;

    private readonly CrearReglaReabastecimientoUseCase _crearReglaUseCase;

    private readonly ActualizarReglaReabastecimientoUseCase _actualizarReglaUseCase;

    private readonly EvaluarReabastecimientoUseCase _evaluarUseCase;

    private readonly ListarSugerenciasReabastecimientoUseCase _listarSugerenciasUseCase;

    private readonly GenerarSolicitudReabastecimientoUseCase _generarSolicitudUseCase;

    private readonly DescartarSugerenciaReabastecimientoUseCase _descartarSugerenciaUseCase;

    private readonly ResumenReabastecimientoUseCase _resumenUseCase;

    public ReabastecimientoAutomaticoController(
        ListarReglasReabastecimientoUseCase listarReglasUseCase,
        ObtenerReglaReabastecimientoUseCase obtenerReglaUseCase,
        CrearReglaReabastecimientoUseCase crearReglaUseCase,
        ActualizarReglaReabastecimientoUseCase actualizarReglaUseCase,
        EvaluarReabastecimientoUseCase evaluarUseCase,
        ListarSugerenciasReabastecimientoUseCase listarSugerenciasUseCase,
        GenerarSolicitudReabastecimientoUseCase generarSolicitudUseCase,
        DescartarSugerenciaReabastecimientoUseCase descartarSugerenciaUseCase,
        ResumenReabastecimientoUseCase resumenUseCase)
    {
        _listarReglasUseCase = listarReglasUseCase;
        _obtenerReglaUseCase = obtenerReglaUseCase;
        _crearReglaUseCase = crearReglaUseCase;
        _actualizarReglaUseCase = actualizarReglaUseCase;
        _evaluarUseCase = evaluarUseCase;
        _listarSugerenciasUseCase = listarSugerenciasUseCase;
        _generarSolicitudUseCase = generarSolicitudUseCase;
        _descartarSugerenciaUseCase = descartarSugerenciaUseCase;
        _resumenUseCase = resumenUseCase;
    }

    [HttpGet("reglas")]
    public async Task<IActionResult> ListarReglas(
        [FromQuery] int? almacenId,
        [FromQuery] int? productoId,
        [FromQuery] bool? activa,
        [FromQuery] bool? soloBajoPuntoReorden,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaReglasReabastecimientoDto resultado = await _listarReglasUseCase.ExecuteAsync(
            almacenId,
            productoId,
            activa,
            soloBajoPuntoReorden,
            busqueda,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("reglas/{id:int}")]
    public async Task<IActionResult> ObtenerRegla(int id)
    {
        ReglaReabastecimientoDetalleDto resultado = await _obtenerReglaUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost("reglas")]
    public async Task<IActionResult> CrearRegla([FromBody] CrearReglaReabastecimientoRequestDto solicitud)
    {
        ReglaReabastecimientoDetalleDto resultado = await _crearReglaUseCase.ExecuteAsync(solicitud);

        return CreatedAtAction(nameof(ObtenerRegla), new { id = resultado.Id }, resultado);
    }

    [HttpPut("reglas/{id:int}")]
    public async Task<IActionResult> ActualizarRegla(
        int id,
        [FromBody] ActualizarReglaReabastecimientoRequestDto solicitud)
    {
        ReglaReabastecimientoListadoDto resultado = await _actualizarReglaUseCase.ExecuteAsync(id, solicitud);

        return Ok(resultado);
    }

    [HttpPost("evaluar")]
    public async Task<IActionResult> Evaluar([FromBody] EvaluarReabastecimientoRequestDto? solicitud)
    {
        ResultadoEvaluacionReabastecimientoDto resultado = await _evaluarUseCase.ExecuteAsync(solicitud);

        return Ok(resultado);
    }

    [HttpGet("sugerencias")]
    public async Task<IActionResult> ListarSugerencias(
        [FromQuery] int? almacenId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaSugerenciasReabastecimientoDto resultado = await _listarSugerenciasUseCase.ExecuteAsync(
            almacenId,
            estado,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpPost("sugerencias/{id:int}/generar-solicitud")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> GenerarSolicitud(int id)
    {
        EjecucionReabastecimientoItemDto resultado = await _generarSolicitudUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost("sugerencias/{id:int}/descartar")]
    public async Task<IActionResult> DescartarSugerencia(
        int id,
        [FromBody] DescartarSugerenciaRequestDto? solicitud)
    {
        EjecucionReabastecimientoItemDto resultado = await _descartarSugerenciaUseCase.ExecuteAsync(id, solicitud);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenReabastecimientoDto resultado = await _resumenUseCase.ExecuteAsync();

        return Ok(resultado);
    }
}
