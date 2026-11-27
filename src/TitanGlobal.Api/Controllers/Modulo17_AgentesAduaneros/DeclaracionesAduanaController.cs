using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

namespace TitanGlobal.Api.Controllers.Modulo17_AgentesAduaneros;

[ApiController]
[Route("api/v1/declaraciones-aduana")]
public class DeclaracionesAduanaController : ControllerBase
{
    private readonly ListarDeclaracionesAduanaUseCase _listarDeclaracionesAduanaUseCase;

    private readonly ObtenerDeclaracionAduanaUseCase _obtenerDeclaracionAduanaUseCase;

    private readonly CrearDeclaracionAduanaUseCase _crearDeclaracionAduanaUseCase;

    private readonly ActualizarDeclaracionAduanaUseCase _actualizarDeclaracionAduanaUseCase;

    private readonly NumerarDeclaracionAduanaUseCase _numerarDeclaracionAduanaUseCase;

    private readonly LevantarDeclaracionAduanaUseCase _levantarDeclaracionAduanaUseCase;

    private readonly AnularDeclaracionAduanaUseCase _anularDeclaracionAduanaUseCase;

    private readonly ResumenDeclaracionesAduanaUseCase _resumenDeclaracionesAduanaUseCase;

    public DeclaracionesAduanaController(
        ListarDeclaracionesAduanaUseCase listarDeclaracionesAduanaUseCase,
        ObtenerDeclaracionAduanaUseCase obtenerDeclaracionAduanaUseCase,
        CrearDeclaracionAduanaUseCase crearDeclaracionAduanaUseCase,
        ActualizarDeclaracionAduanaUseCase actualizarDeclaracionAduanaUseCase,
        NumerarDeclaracionAduanaUseCase numerarDeclaracionAduanaUseCase,
        LevantarDeclaracionAduanaUseCase levantarDeclaracionAduanaUseCase,
        AnularDeclaracionAduanaUseCase anularDeclaracionAduanaUseCase,
        ResumenDeclaracionesAduanaUseCase resumenDeclaracionesAduanaUseCase)
    {
        _listarDeclaracionesAduanaUseCase = listarDeclaracionesAduanaUseCase;
        _obtenerDeclaracionAduanaUseCase = obtenerDeclaracionAduanaUseCase;
        _crearDeclaracionAduanaUseCase = crearDeclaracionAduanaUseCase;
        _actualizarDeclaracionAduanaUseCase = actualizarDeclaracionAduanaUseCase;
        _numerarDeclaracionAduanaUseCase = numerarDeclaracionAduanaUseCase;
        _levantarDeclaracionAduanaUseCase = levantarDeclaracionAduanaUseCase;
        _anularDeclaracionAduanaUseCase = anularDeclaracionAduanaUseCase;
        _resumenDeclaracionesAduanaUseCase = resumenDeclaracionesAduanaUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? canal,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaDeclaracionesDto resultado = await _listarDeclaracionesAduanaUseCase.ExecuteAsync(
            busqueda,
            estado,
            canal,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenDeclaracionesDto resultado = await _resumenDeclaracionesAduanaUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        DeclaracionDetalleDto resultado = await _obtenerDeclaracionAduanaUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearDeclaracionRequestDto solicitud)
    {
        int id = await _crearDeclaracionAduanaUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarDeclaracionRequestDto solicitud)
    {
        int idActualizado = await _actualizarDeclaracionAduanaUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpPost("{id:int}/numerar")]
    public async Task<IActionResult> Numerar(int id, [FromBody] NumerarDeclaracionRequestDto solicitud)
    {
        int idNumerado = await _numerarDeclaracionAduanaUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idNumerado });
    }

    [HttpPost("{id:int}/levantar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Levantar(int id)
    {
        int idLevantado = await _levantarDeclaracionAduanaUseCase.ExecuteAsync(id);

        return Ok(new { Id = idLevantado });
    }

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularDeclaracionRequestDto solicitud)
    {
        int idAnulado = await _anularDeclaracionAduanaUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idAnulado });
    }
}
