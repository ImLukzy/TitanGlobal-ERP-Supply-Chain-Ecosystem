using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.UseCases.Modulo00_Sistema.Queries;

namespace TitanGlobal.Api.Controllers.Modulo00_Sistema;

[ApiController]
[AllowAnonymous]
[Route("api/v1/sistema/salud")]
public class HealthController : ControllerBase
{
    private readonly ISender _sender;

    public HealthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar()
    {
        object resultado = await _sender.Send(new ConsultarSaludQuery());
        return Ok(resultado);
    }
}
