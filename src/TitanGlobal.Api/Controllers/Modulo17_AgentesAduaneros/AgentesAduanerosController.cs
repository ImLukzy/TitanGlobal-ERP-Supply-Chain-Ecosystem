using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

namespace TitanGlobal.Api.Controllers.Modulo17_AgentesAduaneros;

[ApiController]
[Route("api/v1/agentes-aduaneros")]
public class AgentesAduanerosController : ControllerBase
{
    private readonly ListarAgentesAduanerosUseCase _listarAgentesAduanerosUseCase;

    private readonly ObtenerAgenteAduaneroUseCase _obtenerAgenteAduaneroUseCase;

    private readonly CrearAgenteAduaneroUseCase _crearAgenteAduaneroUseCase;

    private readonly ActualizarAgenteAduaneroUseCase _actualizarAgenteAduaneroUseCase;

    private readonly ActivarAgenteAduaneroUseCase _activarAgenteAduaneroUseCase;

    private readonly DesactivarAgenteAduaneroUseCase _desactivarAgenteAduaneroUseCase;

    private readonly EliminarAgenteAduaneroUseCase _eliminarAgenteAduaneroUseCase;

    public AgentesAduanerosController(
        ListarAgentesAduanerosUseCase listarAgentesAduanerosUseCase,
        ObtenerAgenteAduaneroUseCase obtenerAgenteAduaneroUseCase,
        CrearAgenteAduaneroUseCase crearAgenteAduaneroUseCase,
        ActualizarAgenteAduaneroUseCase actualizarAgenteAduaneroUseCase,
        ActivarAgenteAduaneroUseCase activarAgenteAduaneroUseCase,
        DesactivarAgenteAduaneroUseCase desactivarAgenteAduaneroUseCase,
        EliminarAgenteAduaneroUseCase eliminarAgenteAduaneroUseCase)
    {
        _listarAgentesAduanerosUseCase = listarAgentesAduanerosUseCase;
        _obtenerAgenteAduaneroUseCase = obtenerAgenteAduaneroUseCase;
        _crearAgenteAduaneroUseCase = crearAgenteAduaneroUseCase;
        _actualizarAgenteAduaneroUseCase = actualizarAgenteAduaneroUseCase;
        _activarAgenteAduaneroUseCase = activarAgenteAduaneroUseCase;
        _desactivarAgenteAduaneroUseCase = desactivarAgenteAduaneroUseCase;
        _eliminarAgenteAduaneroUseCase = eliminarAgenteAduaneroUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAgentesDto resultado = await _listarAgentesAduanerosUseCase.ExecuteAsync(busqueda, activo, pagina, tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        AgenteDto resultado = await _obtenerAgenteAduaneroUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarAgenteRequestDto solicitud)
    {
        int id = await _crearAgenteAduaneroUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarAgenteRequestDto solicitud)
    {
        int idActualizado = await _actualizarAgenteAduaneroUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpPost("{id:int}/activar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        int idActivado = await _activarAgenteAduaneroUseCase.ExecuteAsync(id);

        return Ok(new { Id = idActivado });
    }

    [HttpPost("{id:int}/desactivar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Desactivar(int id)
    {
        int idDesactivado = await _desactivarAgenteAduaneroUseCase.ExecuteAsync(id);

        return Ok(new { Id = idDesactivado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarAgenteAduaneroUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
