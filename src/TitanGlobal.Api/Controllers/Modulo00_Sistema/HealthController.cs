using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.UseCases.Modulo00_Sistema;

namespace TitanGlobal.Api.Controllers.Modulo00_Sistema;

[ApiController]
[Route("api/v1/sistema/salud")]
public class HealthController : ControllerBase
{
    private readonly ConsultarSaludUseCase _consultarSaludUseCase;

    public HealthController(ConsultarSaludUseCase consultarSaludUseCase)
    {
        _consultarSaludUseCase = consultarSaludUseCase;
    }

    [HttpGet]
    public IActionResult Consultar()
    {
        return Ok(_consultarSaludUseCase.Ejecutar());
    }
}
