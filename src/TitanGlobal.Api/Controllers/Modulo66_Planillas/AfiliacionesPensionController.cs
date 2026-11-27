using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;
using TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

namespace TitanGlobal.Api.Controllers.Modulo66_Planillas;

[ApiController]
[Route("api/v1/afiliaciones-pension")]
public class AfiliacionesPensionController : ControllerBase
{
    private readonly ISender _sender;

    public AfiliacionesPensionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaAfiliacionesPensionDto resultado = await _sender.Send(new ListarAfiliacionesPensionQuery
        {
            EmpleadoId = empleadoId,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpPut("{empleadoId:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Guardar(int empleadoId, [FromBody] GuardarAfiliacionPensionRequestDto solicitud)
    {
        AfiliacionPensionIdDto resultado = await _sender.Send(new GuardarAfiliacionPensionCommand
        {
            EmpleadoId = empleadoId,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }
}
