using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno;

namespace TitanGlobal.Api.Controllers.Modulo68_EvaluacionDesempeno;

[ApiController]
[Route("api/v1/ciclos-evaluacion")]
public class CiclosEvaluacionController : ControllerBase
{
    private readonly AdministrarCiclosEvaluacionUseCase _administrarCiclos;

    public CiclosEvaluacionController(AdministrarCiclosEvaluacionUseCase administrarCiclos)
    {
        _administrarCiclos = administrarCiclos;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaCiclosEvaluacionDto resultado = await _administrarCiclos.ListarAsync(busqueda, estado, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CicloEvaluacionDetalleDto resultado = await _administrarCiclos.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpGet("{id:int}/resultados")]
    public async Task<IActionResult> ObtenerResultados(int id)
    {
        ResultadosCicloEvaluacionDto resultado = await _administrarCiclos.ObtenerResultadosAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarCicloEvaluacionRequestDto solicitud)
    {
        CicloEvaluacionIdDto resultado = await _administrarCiclos.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCicloEvaluacionRequestDto solicitud)
    {
        CicloEvaluacionIdDto resultado = await _administrarCiclos.ActualizarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/competencias")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> CrearCompetencia(int id, [FromBody] GuardarCompetenciaEvaluacionRequestDto solicitud)
    {
        CompetenciaEvaluacionIdDto resultado = await _administrarCiclos.CrearCompetenciaAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}/competencias/{competenciaId:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> ActualizarCompetencia(int id, int competenciaId, [FromBody] GuardarCompetenciaEvaluacionRequestDto solicitud)
    {
        CompetenciaEvaluacionIdDto resultado = await _administrarCiclos.ActualizarCompetenciaAsync(id, competenciaId, solicitud);
        return Ok(resultado);
    }

    [HttpDelete("{id:int}/competencias/{competenciaId:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> EliminarCompetencia(int id, int competenciaId)
    {
        await _administrarCiclos.EliminarCompetenciaAsync(id, competenciaId);
        return NoContent();
    }

    [HttpPost("{id:int}/apertura")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Abrir(int id)
    {
        CicloEvaluacionIdDto resultado = await _administrarCiclos.AbrirAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cierre")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cerrar(int id)
    {
        CicloEvaluacionIdDto resultado = await _administrarCiclos.CerrarAsync(id);
        return Ok(resultado);
    }
}
