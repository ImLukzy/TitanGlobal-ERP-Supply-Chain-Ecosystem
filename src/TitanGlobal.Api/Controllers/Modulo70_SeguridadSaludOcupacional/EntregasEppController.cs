using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

namespace TitanGlobal.Api.Controllers.Modulo70_SeguridadSaludOcupacional;

[ApiController]
[Route("api/v1/entregas-epp")]
public class EntregasEppController : ControllerBase
{
    private readonly ISender _sender;

    public EntregasEppController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] int? equipoId,
        [FromQuery] bool? vencidas,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaEntregasEppDto resultado = await _sender.Send(new ListarEntregasQuery
        {
            EmpleadoId = empleadoId,
            EquipoId = equipoId,
            Vencidas = vencidas,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEntregaEppRequestDto solicitud)
    {
        EntregaEppIdDto resultado = await _sender.Send(new RegistrarEntregaCommand { Solicitud = solicitud });
        return Ok(resultado);
    }
}
