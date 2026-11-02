using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas;

namespace TitanGlobal.Api.Controllers.Modulo78_NotificacionesAlertas;

[ApiController]
[Route("api/v1/alertas")]
public class AlertasController : ControllerBase
{
    private readonly AlertasUseCase _alertasUseCase;

    public AlertasController(AlertasUseCase alertasUseCase)
    {
        _alertasUseCase = alertasUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaAlertasDto>> Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? severidad,
        [FromQuery] bool incluirDescartadas = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAlertasDto respuesta = await _alertasUseCase.ListarAsync(
            tipo,
            severidad,
            incluirDescartadas,
            pagina,
            tamanoPagina);
        return Ok(respuesta);
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenAlertasDto>> Resumen()
    {
        ResumenAlertasDto respuesta = await _alertasUseCase.ResumenAsync();
        return Ok(respuesta);
    }

    [HttpPost("descartar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Descartar([FromBody] ClaveAlertaRequestDto solicitud)
    {
        await _alertasUseCase.DescartarAsync(solicitud);
        return NoContent();
    }

    [HttpPost("restaurar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Restaurar([FromBody] ClaveAlertaRequestDto solicitud)
    {
        await _alertasUseCase.RestaurarAsync(solicitud);
        return NoContent();
    }
}
