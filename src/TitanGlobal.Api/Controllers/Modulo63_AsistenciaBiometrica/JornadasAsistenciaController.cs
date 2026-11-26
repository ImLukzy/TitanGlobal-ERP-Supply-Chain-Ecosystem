using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;

namespace TitanGlobal.Api.Controllers.Modulo63_AsistenciaBiometrica;

[ApiController]
[Route("api/v1/jornadas-asistencia")]
public class JornadasAsistenciaController : ControllerBase
{
    private readonly AdministrarJornadasAsistenciaUseCase _administrarJornadas;

    public JornadasAsistenciaController(AdministrarJornadasAsistenciaUseCase administrarJornadas)
    {
        _administrarJornadas = administrarJornadas;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] string? estado,
        [FromQuery] string? estadoHorasExtra,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaJornadasAsistenciaDto resultado = await _administrarJornadas.ListarAsync(
            empleadoId,
            desde,
            hasta,
            estado,
            estadoHorasExtra,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        ResumenAsistenciaDto resultado = await _administrarJornadas.ResumenAsync(desde, hasta);
        return Ok(resultado);
    }

    [HttpPost("calculo")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Calcular([FromBody] CalcularJornadasRequestDto solicitud)
    {
        ResultadoCalculoDto resultado = await _administrarJornadas.CalcularAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/horas-extra/aprobacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> AprobarHorasExtra(int id)
    {
        JornadaAsistenciaIdDto resultado = await _administrarJornadas.AprobarHorasExtraAsync(id, ObtenerUsuario());
        return Ok(resultado);
    }

    [HttpPost("{id:int}/horas-extra/rechazo")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> RechazarHorasExtra(int id, [FromBody] RechazarHorasExtraRequestDto solicitud)
    {
        JornadaAsistenciaIdDto resultado = await _administrarJornadas.RechazarHorasExtraAsync(id, solicitud, ObtenerUsuario());
        return Ok(resultado);
    }

    // El JWT nombra la reclamación "name" y AutenticacionJwtExtensions la usa como nombre de la identidad.
    private string ObtenerUsuario()
    {
        return User.Identity?.Name ?? string.Empty;
    }
}
