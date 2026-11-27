using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;
using TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

namespace TitanGlobal.Api.Controllers.Modulo66_Planillas;

[ApiController]
[Route("api/v1/bonos-planilla")]
public class BonosPlanillaController : ControllerBase
{
    private readonly ISender _sender;

    public BonosPlanillaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        [FromQuery] int? empleadoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaBonosPlanillaDto resultado = await _sender.Send(new ListarBonosPlanillaQuery
        {
            Anio = anio,
            Mes = mes,
            EmpleadoId = empleadoId,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarBonoPlanillaRequestDto solicitud)
    {
        BonoPlanillaIdDto resultado = await _sender.Send(new CrearBonoPlanillaCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarBonoPlanillaCommand { Id = id });
        return NoContent();
    }
}
