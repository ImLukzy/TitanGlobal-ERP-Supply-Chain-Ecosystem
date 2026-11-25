using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

namespace TitanGlobal.Api.Controllers.Modulo69_Capacitaciones;

[ApiController]
[Route("api/v1/programaciones-curso")]
public class ProgramacionesCursoController : ControllerBase
{
    private readonly AdministrarProgramacionesCursoUseCase _administrarProgramaciones;

    public ProgramacionesCursoController(AdministrarProgramacionesCursoUseCase administrarProgramaciones)
    {
        _administrarProgramaciones = administrarProgramaciones;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? anio,
        [FromQuery] int? cursoId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaProgramacionesDto resultado = await _administrarProgramaciones.ListarAsync(anio, cursoId, estado, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ProgramacionDetalleDto resultado = await _administrarProgramaciones.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarProgramacionRequestDto solicitud)
    {
        ProgramacionIdDto resultado = await _administrarProgramaciones.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/inscripciones")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Inscribir(int id, [FromBody] InscribirEmpleadoRequestDto solicitud)
    {
        InscripcionIdDto resultado = await _administrarProgramaciones.InscribirAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/inscripciones/{inscripcionId:int}/retiro")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Retirar(int id, int inscripcionId)
    {
        InscripcionIdDto resultado = await _administrarProgramaciones.RetirarAsync(id, inscripcionId);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/realizacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Realizar(int id)
    {
        ProgramacionIdDto resultado = await _administrarProgramaciones.RealizarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cancelacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cancelar(int id)
    {
        ProgramacionIdDto resultado = await _administrarProgramaciones.CancelarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/resultados")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> RegistrarResultados(int id, [FromBody] RegistrarResultadosRequestDto solicitud)
    {
        ProgramacionIdDto resultado = await _administrarProgramaciones.RegistrarResultadosAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/inscripciones/{inscripcionId:int}/certificacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> EmitirCertificacion(int id, int inscripcionId, [FromBody] EmitirCertificacionRequestDto solicitud)
    {
        CertificacionIdDto resultado = await _administrarProgramaciones.EmitirCertificacionAsync(id, inscripcionId, solicitud);
        return Ok(resultado);
    }
}
