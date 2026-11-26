using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;

namespace TitanGlobal.Api.Controllers.Modulo64_TurnosHorarios;

[ApiController]
[Route("api/v1/asignaciones-turno")]
public class AsignacionesTurnoController : ControllerBase
{
    private readonly AdministrarAsignacionesTurnoUseCase _administrarAsignaciones;

    public AsignacionesTurnoController(AdministrarAsignacionesTurnoUseCase administrarAsignaciones)
    {
        _administrarAsignaciones = administrarAsignaciones;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] int? turnoId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaAsignacionesTurnoDto resultado = await _administrarAsignaciones.ListarAsync(
            empleadoId,
            turnoId,
            estado,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("programacion")]
    public async Task<IActionResult> Programacion(
        [FromQuery] int? empleadoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta)
    {
        List<DiaProgramadoDto> resultado = await _administrarAsignaciones.ProgramacionAsync(empleadoId, desde, hasta);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarAsignacionTurnoRequestDto solicitud)
    {
        AsignacionTurnoIdDto resultado = await _administrarAsignaciones.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/finalizacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Finalizar(int id, [FromBody] FinalizarAsignacionTurnoRequestDto solicitud)
    {
        AsignacionTurnoIdDto resultado = await _administrarAsignaciones.FinalizarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _administrarAsignaciones.EliminarAsync(id);
        return NoContent();
    }
}
