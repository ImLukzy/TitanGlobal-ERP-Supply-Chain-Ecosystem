using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Application.UseCases.Modulo66_Planillas;

namespace TitanGlobal.Api.Controllers.Modulo66_Planillas;

[ApiController]
[Route("api/v1/afiliaciones-pension")]
public class AfiliacionesPensionController : ControllerBase
{
    private readonly AdministrarAfiliacionesPensionUseCase _administrarAfiliaciones;

    public AfiliacionesPensionController(AdministrarAfiliacionesPensionUseCase administrarAfiliaciones)
    {
        _administrarAfiliaciones = administrarAfiliaciones;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaAfiliacionesPensionDto resultado = await _administrarAfiliaciones.ListarAsync(empleadoId, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPut("{empleadoId:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Guardar(int empleadoId, [FromBody] GuardarAfiliacionPensionRequestDto solicitud)
    {
        AfiliacionPensionIdDto resultado = await _administrarAfiliaciones.GuardarAsync(empleadoId, solicitud);
        return Ok(resultado);
    }
}
