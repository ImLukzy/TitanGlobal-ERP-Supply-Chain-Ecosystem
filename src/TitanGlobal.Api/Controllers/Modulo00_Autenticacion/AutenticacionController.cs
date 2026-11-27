using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.UseCases.Modulo00_Autenticacion.Queries;

namespace TitanGlobal.Api.Controllers.Modulo00_Autenticacion;

[ApiController]
[Route("api/v1/autenticacion")]
public class AutenticacionController : ControllerBase
{
    private readonly ISender _sender;

    public AutenticacionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> IniciarSesion([FromBody] IniciarSesionRequestDto solicitud)
    {
        SesionDto sesion = await _sender.Send(new IniciarSesionQuery { Solicitud = solicitud });
        return Ok(sesion);
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        PerfilDto perfil = await _sender.Send(new ObtenerPerfilQuery
        {
            IdentificadorToken = User.FindFirst("sub")?.Value
        });
        return Ok(perfil);
    }
}
