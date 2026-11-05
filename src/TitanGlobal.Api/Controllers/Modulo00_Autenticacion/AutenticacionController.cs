using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.UseCases.Modulo00_Autenticacion;

namespace TitanGlobal.Api.Controllers.Modulo00_Autenticacion;

[ApiController]
[Route("api/v1/autenticacion")]
public class AutenticacionController : ControllerBase
{
    private readonly IniciarSesionUseCase _iniciarSesion;
    private readonly ObtenerPerfilUseCase _obtenerPerfil;

    public AutenticacionController(IniciarSesionUseCase iniciarSesion, ObtenerPerfilUseCase obtenerPerfil)
    {
        _iniciarSesion = iniciarSesion;
        _obtenerPerfil = obtenerPerfil;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> IniciarSesion([FromBody] IniciarSesionRequestDto solicitud)
    {
        SesionDto sesion = await _iniciarSesion.ExecuteAsync(solicitud);
        return Ok(sesion);
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        PerfilDto perfil = await _obtenerPerfil.ExecuteAsync(User.FindFirst("sub")?.Value);
        return Ok(perfil);
    }
}
