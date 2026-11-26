using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

namespace TitanGlobal.Api.Controllers.Modulo37_ControlCalidadProductoTerminado;

[ApiController]
[Route("api/v1/inspecciones-producto-terminado")]
public class InspeccionesProductoTerminadoController : ControllerBase
{
    private readonly ListarInspeccionesProductoTerminadoUseCase _listar;
    private readonly ResumenInspeccionesProductoTerminadoUseCase _resumen;
    private readonly AvanceInspeccionOrdenUseCase _avance;
    private readonly ObtenerInspeccionProductoTerminadoUseCase _obtener;

    public InspeccionesProductoTerminadoController(
        ListarInspeccionesProductoTerminadoUseCase listar,
        ResumenInspeccionesProductoTerminadoUseCase resumen,
        AvanceInspeccionOrdenUseCase avance,
        ObtenerInspeccionProductoTerminadoUseCase obtener)
    {
        _listar = listar;
        _resumen = resumen;
        _avance = avance;
        _obtener = obtener;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? ordenProduccionId,
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaInspeccionesProductoTerminadoDto resultado = await _listar.ExecuteAsync(
            busqueda, ordenProduccionId, productoId, estado, desde, hasta, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenInspeccionesProductoTerminadoDto resultado = await _resumen.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("ordenes/{ordenProduccionId:int}/avance")]
    public async Task<IActionResult> Avance(int ordenProduccionId)
    {
        AvanceInspeccionOrdenDto resultado = await _avance.ExecuteAsync(ordenProduccionId);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        InspeccionProductoTerminadoDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }
}
