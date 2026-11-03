using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Application.UseCases.Modulo42_Conductores;

namespace TitanGlobal.Api.Controllers.Modulo42_Conductores;

[ApiController]
[Route("api/v1/conductores")]
public class ConductoresController : ControllerBase
{
    private readonly ListarConductoresUseCase _listarConductoresUseCase;

    private readonly ObtenerConductorUseCase _obtenerConductorUseCase;

    private readonly CrearConductorUseCase _crearConductorUseCase;

    private readonly ActualizarConductorUseCase _actualizarConductorUseCase;

    private readonly EliminarConductorUseCase _eliminarConductorUseCase;

    public ConductoresController(
        ListarConductoresUseCase listarConductoresUseCase,
        ObtenerConductorUseCase obtenerConductorUseCase,
        CrearConductorUseCase crearConductorUseCase,
        ActualizarConductorUseCase actualizarConductorUseCase,
        EliminarConductorUseCase eliminarConductorUseCase)
    {
        _listarConductoresUseCase = listarConductoresUseCase;
        _obtenerConductorUseCase = obtenerConductorUseCase;
        _crearConductorUseCase = crearConductorUseCase;
        _actualizarConductorUseCase = actualizarConductorUseCase;
        _eliminarConductorUseCase = eliminarConductorUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? habilitado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaConductoresDto resultado = await _listarConductoresUseCase.ExecuteAsync(
            busqueda,
            habilitado,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ConductorDetalleDto resultado = await _obtenerConductorUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarConductorRequestDto solicitud)
    {
        int id = await _crearConductorUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarConductorRequestDto solicitud)
    {
        int idActualizado = await _actualizarConductorUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarConductorUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
