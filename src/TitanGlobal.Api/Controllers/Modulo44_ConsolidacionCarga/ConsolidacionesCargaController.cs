using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

namespace TitanGlobal.Api.Controllers.Modulo44_ConsolidacionCarga;

[ApiController]
[Route("api/v1/consolidaciones-carga")]
public class ConsolidacionesCargaController : ControllerBase
{
    private readonly ListarConsolidacionesUseCase _listar;
    private readonly ObtenerConsolidacionUseCase _obtener;
    private readonly CrearConsolidacionUseCase _crear;
    private readonly ActualizarConsolidacionUseCase _actualizar;
    private readonly EliminarConsolidacionUseCase _eliminar;
    private readonly ConfirmarConsolidacionUseCase _confirmar;
    private readonly CancelarConsolidacionUseCase _cancelar;
    private readonly ResumenConsolidacionesUseCase _resumen;
    public ConsolidacionesCargaController(ListarConsolidacionesUseCase listar, ObtenerConsolidacionUseCase obtener, CrearConsolidacionUseCase crear, ActualizarConsolidacionUseCase actualizar, EliminarConsolidacionUseCase eliminar, ConfirmarConsolidacionUseCase confirmar, CancelarConsolidacionUseCase cancelar, ResumenConsolidacionesUseCase resumen)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _eliminar = eliminar;
        _confirmar = confirmar;
        _cancelar = cancelar;
        _resumen = resumen;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaConsolidacionesDto resultado = await _listar.ExecuteAsync(busqueda, estado, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ConsolidacionDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarConsolidacionRequestDto solicitud)
    {
        int resultado = await _crear.ExecuteAsync(solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarConsolidacionRequestDto solicitud)
    {
        int resultado = await _actualizar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminar.ExecuteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/confirmacion")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Confirmar(int id)
    {
        int resultado = await _confirmar.ExecuteAsync(id);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/cancelacion")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarConsolidacionRequestDto solicitud)
    {
        int resultado = await _cancelar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenConsolidacionesDto resultado = await _resumen.ExecuteAsync();
        return Ok(resultado);
    }
}
