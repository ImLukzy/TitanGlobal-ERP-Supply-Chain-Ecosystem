using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.UseCases.Modulo00_Autenticacion;

namespace TitanGlobal.Api.Controllers.Modulo00_Autenticacion;

[ApiController]
[Route("api/v1/usuarios")]
public class ContrasenasController : ControllerBase
{
    private readonly CambiarContrasenaUseCase _cambiarContrasena;

    public ContrasenasController(CambiarContrasenaUseCase cambiarContrasena)
    {
        _cambiarContrasena = cambiarContrasena;
    }

    [HttpPut("{id:int}/contrasena")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Cambiar(int id, [FromBody] CambiarContrasenaRequestDto solicitud)
    {
        await _cambiarContrasena.ExecuteAsync(id, solicitud);
        return NoContent();
    }
}
