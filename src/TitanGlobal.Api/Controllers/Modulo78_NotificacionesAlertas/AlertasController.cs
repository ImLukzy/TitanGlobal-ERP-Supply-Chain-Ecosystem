using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas.Commands;
using TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas.Queries;

namespace TitanGlobal.Api.Controllers.Modulo78_NotificacionesAlertas;

[ApiController]
[Route("api/v1/alertas")]
public class AlertasController : ControllerBase
{
    private readonly ISender _sender;

    public AlertasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaAlertasDto>> Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? severidad,
        [FromQuery] bool incluirDescartadas = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAlertasDto respuesta = await _sender.Send(new ListarAlertasQuery
        {
            Tipo = tipo,
            Severidad = severidad,
            IncluirDescartadas = incluirDescartadas,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(respuesta);
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenAlertasDto>> Resumen()
    {
        ResumenAlertasDto respuesta = await _sender.Send(new ResumenAlertasQuery());
        return Ok(respuesta);
    }

    [HttpPost("descartar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Descartar([FromBody] ClaveAlertaRequestDto solicitud)
    {
        await _sender.Send(new DescartarAlertaCommand { Solicitud = solicitud });
        return NoContent();
    }

    [HttpPost("restaurar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restaurar([FromBody] ClaveAlertaRequestDto solicitud)
    {
        await _sender.Send(new RestaurarAlertaCommand { Solicitud = solicitud });
        return NoContent();
    }
}
