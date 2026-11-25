using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

namespace TitanGlobal.Api.Controllers.Modulo70_SeguridadSaludOcupacional;

[ApiController]
[Route("api/v1/entregas-epp")]
public class EntregasEppController : ControllerBase
{
    private readonly AdministrarEquiposProteccionUseCase _administrarEquipos;

    public EntregasEppController(AdministrarEquiposProteccionUseCase administrarEquipos)
    {
        _administrarEquipos = administrarEquipos;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] int? equipoId,
        [FromQuery] bool? vencidas,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaEntregasEppDto resultado = await _administrarEquipos.ListarEntregasAsync(empleadoId, equipoId, vencidas, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEntregaEppRequestDto solicitud)
    {
        EntregaEppIdDto resultado = await _administrarEquipos.RegistrarEntregaAsync(solicitud);
        return Ok(resultado);
    }
}
