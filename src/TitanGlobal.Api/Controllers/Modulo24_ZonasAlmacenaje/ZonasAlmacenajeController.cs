using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje;

namespace TitanGlobal.Api.Controllers.Modulo24_ZonasAlmacenaje;

[ApiController]
[Route("api/v1/zonas-almacenaje")]
public class ZonasAlmacenajeController : ControllerBase
{
    private readonly ListarZonasAlmacenajeUseCase _listarZonasAlmacenajeUseCase;

    private readonly ObtenerZonaAlmacenajeUseCase _obtenerZonaAlmacenajeUseCase;

    private readonly CrearZonaAlmacenajeUseCase _crearZonaAlmacenajeUseCase;

    private readonly ActualizarZonaAlmacenajeUseCase _actualizarZonaAlmacenajeUseCase;

    private readonly EliminarZonaAlmacenajeUseCase _eliminarZonaAlmacenajeUseCase;

    private readonly ResumenZonasAlmacenajeUseCase _resumenZonasAlmacenajeUseCase;

    public ZonasAlmacenajeController(
        ListarZonasAlmacenajeUseCase listarZonasAlmacenajeUseCase,
        ObtenerZonaAlmacenajeUseCase obtenerZonaAlmacenajeUseCase,
        CrearZonaAlmacenajeUseCase crearZonaAlmacenajeUseCase,
        ActualizarZonaAlmacenajeUseCase actualizarZonaAlmacenajeUseCase,
        EliminarZonaAlmacenajeUseCase eliminarZonaAlmacenajeUseCase,
        ResumenZonasAlmacenajeUseCase resumenZonasAlmacenajeUseCase)
    {
        _listarZonasAlmacenajeUseCase = listarZonasAlmacenajeUseCase;
        _obtenerZonaAlmacenajeUseCase = obtenerZonaAlmacenajeUseCase;
        _crearZonaAlmacenajeUseCase = crearZonaAlmacenajeUseCase;
        _actualizarZonaAlmacenajeUseCase = actualizarZonaAlmacenajeUseCase;
        _eliminarZonaAlmacenajeUseCase = eliminarZonaAlmacenajeUseCase;
        _resumenZonasAlmacenajeUseCase = resumenZonasAlmacenajeUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? almacenId,
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] string? temperatura,
        [FromQuery] bool? activa,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaZonasDto resultado = await _listarZonasAlmacenajeUseCase.ExecuteAsync(
            almacenId,
            busqueda,
            tipo,
            temperatura,
            activa,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] int? almacenId)
    {
        ResumenZonasDto resultado = await _resumenZonasAlmacenajeUseCase.ExecuteAsync(almacenId);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ZonaDetalleDto resultado = await _obtenerZonaAlmacenajeUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarZonaRequestDto solicitud)
    {
        int id = await _crearZonaAlmacenajeUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarZonaRequestDto solicitud)
    {
        int idActualizado = await _actualizarZonaAlmacenajeUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarZonaAlmacenajeUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
