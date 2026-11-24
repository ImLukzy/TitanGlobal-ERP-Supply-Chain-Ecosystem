using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

namespace TitanGlobal.Api.Controllers.Modulo12_EvaluacionProveedores;

[ApiController]
[Route("api/v1/evaluaciones-proveedores")]
public class EvaluacionesProveedorController : ControllerBase
{
    private readonly ListarEvaluacionesProveedorUseCase _listarEvaluacionesUseCase;

    private readonly ObtenerEvaluacionProveedorUseCase _obtenerEvaluacionUseCase;

    private readonly CrearEvaluacionProveedorUseCase _crearEvaluacionUseCase;

    private readonly ActualizarEvaluacionProveedorUseCase _actualizarEvaluacionUseCase;

    private readonly EliminarEvaluacionProveedorUseCase _eliminarEvaluacionUseCase;

    private readonly ResumenEvaluacionesProveedorUseCase _resumenEvaluacionesUseCase;

    public EvaluacionesProveedorController(
        ListarEvaluacionesProveedorUseCase listarEvaluacionesUseCase,
        ObtenerEvaluacionProveedorUseCase obtenerEvaluacionUseCase,
        CrearEvaluacionProveedorUseCase crearEvaluacionUseCase,
        ActualizarEvaluacionProveedorUseCase actualizarEvaluacionUseCase,
        EliminarEvaluacionProveedorUseCase eliminarEvaluacionUseCase,
        ResumenEvaluacionesProveedorUseCase resumenEvaluacionesUseCase)
    {
        _listarEvaluacionesUseCase = listarEvaluacionesUseCase;
        _obtenerEvaluacionUseCase = obtenerEvaluacionUseCase;
        _crearEvaluacionUseCase = crearEvaluacionUseCase;
        _actualizarEvaluacionUseCase = actualizarEvaluacionUseCase;
        _eliminarEvaluacionUseCase = eliminarEvaluacionUseCase;
        _resumenEvaluacionesUseCase = resumenEvaluacionesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? proveedorId,
        [FromQuery] string? periodo,
        [FromQuery] string? calificacion,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaEvaluacionesDto resultado = await _listarEvaluacionesUseCase.ExecuteAsync(
            proveedorId,
            periodo,
            calificacion,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenEvaluacionesDto resultado = await _resumenEvaluacionesUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        EvaluacionDetalleDto resultado = await _obtenerEvaluacionUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearEvaluacionRequestDto solicitud)
    {
        int id = await _crearEvaluacionUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarEvaluacionRequestDto solicitud)
    {
        int idActualizado = await _actualizarEvaluacionUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarEvaluacionUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
