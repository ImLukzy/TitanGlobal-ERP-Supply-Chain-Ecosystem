using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Queries;

namespace TitanGlobal.Api.Controllers.Modulo64_TurnosHorarios;

[ApiController]
[Route("api/v1/asignaciones-turno")]
public class AsignacionesTurnoController : ControllerBase
{
    private readonly ISender _sender;

    public AsignacionesTurnoController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] int? turnoId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaAsignacionesTurnoDto resultado = await _sender.Send(new ListarAsignacionesTurnoQuery
        {
            EmpleadoId = empleadoId,
            TurnoId = turnoId,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("programacion")]
    public async Task<IActionResult> Programacion(
        [FromQuery] int? empleadoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta)
    {
        List<DiaProgramadoDto> resultado = await _sender.Send(new ObtenerProgramacionTurnosQuery
        {
            EmpleadoId = empleadoId,
            Desde = desde,
            Hasta = hasta
        });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarAsignacionTurnoRequestDto solicitud)
    {
        AsignacionTurnoIdDto resultado = await _sender.Send(new CrearAsignacionTurnoCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/finalizacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Finalizar(int id, [FromBody] FinalizarAsignacionTurnoRequestDto solicitud)
    {
        AsignacionTurnoIdDto resultado = await _sender.Send(new FinalizarAsignacionTurnoCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarAsignacionTurnoCommand { Id = id });
        return NoContent();
    }
}
