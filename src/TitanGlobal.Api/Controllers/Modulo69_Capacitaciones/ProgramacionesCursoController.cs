using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;
using TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

namespace TitanGlobal.Api.Controllers.Modulo69_Capacitaciones;

[ApiController]
[Route("api/v1/programaciones-curso")]
public class ProgramacionesCursoController : ControllerBase
{
    private readonly ISender _sender;

    public ProgramacionesCursoController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? anio,
        [FromQuery] int? cursoId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaProgramacionesDto resultado = await _sender.Send(new ListarProgramacionesCursoQuery
        {
            Anio = anio,
            CursoId = cursoId,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ProgramacionDetalleDto resultado = await _sender.Send(new ObtenerProgramacionCursoQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarProgramacionRequestDto solicitud)
    {
        ProgramacionIdDto resultado = await _sender.Send(new CrearProgramacionCursoCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/inscripciones")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Inscribir(int id, [FromBody] InscribirEmpleadoRequestDto solicitud)
    {
        InscripcionIdDto resultado = await _sender.Send(new InscribirProgramacionCursoCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/inscripciones/{inscripcionId:int}/retiro")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Retirar(int id, int inscripcionId)
    {
        InscripcionIdDto resultado = await _sender.Send(new RetirarProgramacionCursoCommand
        {
            Id = id,
            InscripcionId = inscripcionId
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/realizacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Realizar(int id)
    {
        ProgramacionIdDto resultado = await _sender.Send(new RealizarProgramacionCursoCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cancelacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cancelar(int id)
    {
        ProgramacionIdDto resultado = await _sender.Send(new CancelarProgramacionCursoCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/resultados")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> RegistrarResultados(int id, [FromBody] RegistrarResultadosRequestDto solicitud)
    {
        ProgramacionIdDto resultado = await _sender.Send(new RegistrarResultadosCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/inscripciones/{inscripcionId:int}/certificacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> EmitirCertificacion(int id, int inscripcionId, [FromBody] EmitirCertificacionRequestDto solicitud)
    {
        CertificacionIdDto resultado = await _sender.Send(new EmitirCertificacionCommand
        {
            Id = id,
            InscripcionId = inscripcionId,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }
}
