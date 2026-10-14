using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo00_Sistema.UseCases;

namespace TitanGlobal.Api.Modules.Modulo00_Sistema.Controllers;

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
