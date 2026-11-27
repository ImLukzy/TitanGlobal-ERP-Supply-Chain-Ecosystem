using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Commands;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Queries;

namespace TitanGlobal.Api.Controllers.Modulo63_AsistenciaBiometrica;

[ApiController]
[Route("api/v1/jornadas-asistencia")]
public class JornadasAsistenciaController : ControllerBase
{
    private readonly ISender _sender;

    public JornadasAsistenciaController(ISender sender)
    {
        _sender = sender;
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
        PaginaJornadasAsistenciaDto resultado = await _sender.Send(new ListarJornadasAsistenciaQuery
        {
            EmpleadoId = empleadoId,
            Desde = desde,
            Hasta = hasta,
            Estado = estado,
            EstadoHorasExtra = estadoHorasExtra,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        ResumenAsistenciaDto resultado = await _sender.Send(new ResumenJornadasAsistenciaQuery
        {
            Desde = desde,
            Hasta = hasta
        });
        return Ok(resultado);
    }

    [HttpPost("calculo")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Calcular([FromBody] CalcularJornadasRequestDto solicitud)
    {
        ResultadoCalculoDto resultado = await _sender.Send(new CalcularJornadasAsistenciaCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/horas-extra/aprobacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> AprobarHorasExtra(int id)
    {
        JornadaAsistenciaIdDto resultado = await _sender.Send(new AprobarHorasExtraCommand
        {
            Id = id,
            Usuario = ObtenerUsuario()
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/horas-extra/rechazo")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> RechazarHorasExtra(int id, [FromBody] RechazarHorasExtraRequestDto solicitud)
    {
        JornadaAsistenciaIdDto resultado = await _sender.Send(new RechazarHorasExtraCommand
        {
            Id = id,
            Solicitud = solicitud,
            Usuario = ObtenerUsuario()
        });
        return Ok(resultado);
    }

    // El JWT nombra la reclamación "name" y AutenticacionJwtExtensions la usa como nombre de la identidad.
    private string ObtenerUsuario()
    {
        return User.Identity?.Name ?? string.Empty;
    }
}
