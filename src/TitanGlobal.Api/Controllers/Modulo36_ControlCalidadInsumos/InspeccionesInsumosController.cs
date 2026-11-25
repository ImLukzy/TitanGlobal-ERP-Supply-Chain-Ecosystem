using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

namespace TitanGlobal.Api.Controllers.Modulo36_ControlCalidadInsumos;

[ApiController]
[Route("api/v1/inspecciones-insumos")]
public class InspeccionesInsumosController : ControllerBase
{
    private readonly ListarInspeccionesInsumosUseCase _listar;
    private readonly ResumenInspeccionesInsumosUseCase _resumen;
    private readonly ObtenerInspeccionInsumoUseCase _obtener;
    private readonly CrearInspeccionInsumoUseCase _crear;
    private readonly EditarInspeccionInsumoUseCase _editar;
    private readonly DictaminarInspeccionInsumoUseCase _dictaminar;
    private readonly AnularInspeccionInsumoUseCase _anular;

    public InspeccionesInsumosController(
        ListarInspeccionesInsumosUseCase listar,
        ResumenInspeccionesInsumosUseCase resumen,
        ObtenerInspeccionInsumoUseCase obtener,
        CrearInspeccionInsumoUseCase crear,
        EditarInspeccionInsumoUseCase editar,
        DictaminarInspeccionInsumoUseCase dictaminar,
        AnularInspeccionInsumoUseCase anular)
    {
        _listar = listar;
        _resumen = resumen;
        _obtener = obtener;
        _crear = crear;
        _editar = editar;
        _dictaminar = dictaminar;
        _anular = anular;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? productoId,
        [FromQuery] int? proveedorId,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaInspeccionesInsumosDto resultado = await _listar.ExecuteAsync(
            busqueda, productoId, proveedorId, estado, desde, hasta, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenInspeccionesInsumosDto resultado = await _resumen.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        InspeccionInsumoDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarInspeccionInsumoRequestDto solicitud)
    {
        InspeccionInsumoCreadoDto resultado = await _crear.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Id }, resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] GuardarInspeccionInsumoRequestDto solicitud)
    {
        int resultado = await _editar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/dictaminar")]
    public async Task<IActionResult> Dictaminar(int id, [FromBody] DictaminarInspeccionInsumoRequestDto solicitud)
    {
        int resultado = await _dictaminar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularInspeccionInsumoRequestDto solicitud)
    {
        int resultado = await _anular.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }
}
