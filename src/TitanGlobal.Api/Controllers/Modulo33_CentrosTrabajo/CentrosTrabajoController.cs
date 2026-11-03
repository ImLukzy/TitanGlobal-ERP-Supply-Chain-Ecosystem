using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo;

namespace TitanGlobal.Api.Controllers.Modulo33_CentrosTrabajo;

[ApiController]
[Route("api/v1/centros-trabajo")]
public class CentrosTrabajoController : ControllerBase
{
    private readonly ListarCentrosTrabajoUseCase _listarCentrosTrabajoUseCase;

    private readonly ObtenerCentroTrabajoUseCase _obtenerCentroTrabajoUseCase;

    private readonly CrearCentroTrabajoUseCase _crearCentroTrabajoUseCase;

    private readonly ActualizarCentroTrabajoUseCase _actualizarCentroTrabajoUseCase;

    private readonly EliminarCentroTrabajoUseCase _eliminarCentroTrabajoUseCase;

    private readonly ListarPlantasCentroTrabajoUseCase _listarPlantasCentroTrabajoUseCase;

    private readonly ResumenCentrosTrabajoUseCase _resumenCentrosTrabajoUseCase;

    public CentrosTrabajoController(
        ListarCentrosTrabajoUseCase listarCentrosTrabajoUseCase,
        ObtenerCentroTrabajoUseCase obtenerCentroTrabajoUseCase,
        CrearCentroTrabajoUseCase crearCentroTrabajoUseCase,
        ActualizarCentroTrabajoUseCase actualizarCentroTrabajoUseCase,
        EliminarCentroTrabajoUseCase eliminarCentroTrabajoUseCase,
        ListarPlantasCentroTrabajoUseCase listarPlantasCentroTrabajoUseCase,
        ResumenCentrosTrabajoUseCase resumenCentrosTrabajoUseCase)
    {
        _listarCentrosTrabajoUseCase = listarCentrosTrabajoUseCase;
        _obtenerCentroTrabajoUseCase = obtenerCentroTrabajoUseCase;
        _crearCentroTrabajoUseCase = crearCentroTrabajoUseCase;
        _actualizarCentroTrabajoUseCase = actualizarCentroTrabajoUseCase;
        _eliminarCentroTrabajoUseCase = eliminarCentroTrabajoUseCase;
        _listarPlantasCentroTrabajoUseCase = listarPlantasCentroTrabajoUseCase;
        _resumenCentrosTrabajoUseCase = resumenCentrosTrabajoUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? planta,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaCentrosTrabajoDto resultado = await _listarCentrosTrabajoUseCase.ExecuteAsync(
            busqueda,
            planta,
            tipo,
            activo,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("plantas")]
    public async Task<IActionResult> Plantas()
    {
        List<string> resultado = await _listarPlantasCentroTrabajoUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenCentrosTrabajoDto resultado = await _resumenCentrosTrabajoUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CentroTrabajoDetalleDto resultado = await _obtenerCentroTrabajoUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarCentroTrabajoRequestDto solicitud)
    {
        int id = await _crearCentroTrabajoUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCentroTrabajoRequestDto solicitud)
    {
        int idActualizado = await _actualizarCentroTrabajoUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarCentroTrabajoUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
