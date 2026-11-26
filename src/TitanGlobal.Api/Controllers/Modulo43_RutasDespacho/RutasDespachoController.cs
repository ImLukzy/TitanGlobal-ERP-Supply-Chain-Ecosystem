using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

namespace TitanGlobal.Api.Controllers.Modulo43_RutasDespacho;

[ApiController]
[Route("api/v1/rutas-despacho")]
public class RutasDespachoController : ControllerBase
{
    private readonly ListarRutasDespachoUseCase _listar;
    private readonly ObtenerRutaDespachoUseCase _obtener;
    private readonly CrearRutaDespachoUseCase _crear;
    private readonly ActualizarRutaDespachoUseCase _actualizar;
    private readonly EliminarRutaDespachoUseCase _eliminar;
    private readonly ListarZonasRutaDespachoUseCase _zonas;
    private readonly ResumenRutasDespachoUseCase _resumen;

    public RutasDespachoController(ListarRutasDespachoUseCase listar, ObtenerRutaDespachoUseCase obtener, CrearRutaDespachoUseCase crear, ActualizarRutaDespachoUseCase actualizar, EliminarRutaDespachoUseCase eliminar, ListarZonasRutaDespachoUseCase zonas, ResumenRutasDespachoUseCase resumen)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _eliminar = eliminar;
        _zonas = zonas;
        _resumen = resumen;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? zona, [FromQuery] bool? activo, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaRutasDespachoDto resultado = await _listar.ExecuteAsync(busqueda, zona, activo, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        RutaDespachoDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarRutaDespachoRequestDto solicitud)
    {
        int resultado = await _crear.ExecuteAsync(solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarRutaDespachoRequestDto solicitud)
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

    [HttpGet("zonas")]
    public async Task<IActionResult> Zonas()
    {
        List<string> resultado = await _zonas.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenRutasDespachoDto resultado = await _resumen.ExecuteAsync();
        return Ok(resultado);
    }
}
