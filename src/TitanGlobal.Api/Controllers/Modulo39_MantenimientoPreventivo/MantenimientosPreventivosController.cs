using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

namespace TitanGlobal.Api.Controllers.Modulo39_MantenimientoPreventivo;

[ApiController]
[Route("api/v1/mantenimientos-preventivos")]
public class MantenimientosPreventivosController : ControllerBase
{
    private readonly ListarMantenimientosPreventivosUseCase _listar;
    private readonly ResumenMantenimientosPreventivosUseCase _resumen;
    private readonly ObtenerMantenimientoPreventivoUseCase _obtener;
    private readonly CrearMantenimientoPreventivoUseCase _crear;
    private readonly EditarMantenimientoPreventivoUseCase _editar;
    private readonly IniciarMantenimientoPreventivoUseCase _iniciar;
    private readonly CompletarMantenimientoPreventivoUseCase _completar;
    private readonly CancelarMantenimientoPreventivoUseCase _cancelar;

    public MantenimientosPreventivosController(
        ListarMantenimientosPreventivosUseCase listar,
        ResumenMantenimientosPreventivosUseCase resumen,
        ObtenerMantenimientoPreventivoUseCase obtener,
        CrearMantenimientoPreventivoUseCase crear,
        EditarMantenimientoPreventivoUseCase editar,
        IniciarMantenimientoPreventivoUseCase iniciar,
        CompletarMantenimientoPreventivoUseCase completar,
        CancelarMantenimientoPreventivoUseCase cancelar)
    {
        _listar = listar;
        _resumen = resumen;
        _obtener = obtener;
        _crear = crear;
        _editar = editar;
        _iniciar = iniciar;
        _completar = completar;
        _cancelar = cancelar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? centroTrabajoId,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaMantenimientosPreventivosDto resultado = await _listar.ExecuteAsync(
            busqueda, centroTrabajoId, estado, desde, hasta, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenMantenimientosPreventivosDto resultado = await _resumen.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        MantenimientoPreventivoDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearMantenimientoPreventivoRequestDto solicitud)
    {
        MantenimientoPreventivoCreadoDto resultado = await _crear.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Id }, resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarMantenimientoPreventivoRequestDto solicitud)
    {
        int resultado = await _editar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/iniciar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Iniciar(int id)
    {
        int resultado = await _iniciar.ExecuteAsync(id);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/completar")]
    public async Task<IActionResult> Completar(int id, [FromBody] CompletarMantenimientoPreventivoRequestDto solicitud)
    {
        int resultado = await _completar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarMantenimientoPreventivoRequestDto solicitud)
    {
        int resultado = await _cancelar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }
}
