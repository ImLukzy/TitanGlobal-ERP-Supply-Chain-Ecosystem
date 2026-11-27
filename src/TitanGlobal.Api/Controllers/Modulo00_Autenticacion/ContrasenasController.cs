using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.UseCases.Modulo00_Autenticacion.Commands;

namespace TitanGlobal.Api.Controllers.Modulo00_Autenticacion;

[ApiController]
[Route("api/v1/usuarios")]
public class ContrasenasController : ControllerBase
{
    private readonly ISender _sender;

    public ContrasenasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPut("{id:int}/contrasena")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Cambiar(int id, [FromBody] CambiarContrasenaRequestDto solicitud)
    {
        await _sender.Send(new CambiarContrasenaCommand { UsuarioId = id, Solicitud = solicitud });
        return NoContent();
    }
}
