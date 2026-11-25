using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Application.UseCases.Modulo66_Planillas;

namespace TitanGlobal.Api.Controllers.Modulo66_Planillas;

[ApiController]
[Route("api/v1/bonos-planilla")]
public class BonosPlanillaController : ControllerBase
{
    private readonly AdministrarBonosPlanillaUseCase _administrarBonos;

    public BonosPlanillaController(AdministrarBonosPlanillaUseCase administrarBonos)
    {
        _administrarBonos = administrarBonos;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        [FromQuery] int? empleadoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaBonosPlanillaDto resultado = await _administrarBonos.ListarAsync(anio, mes, empleadoId, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarBonoPlanillaRequestDto solicitud)
    {
        BonoPlanillaIdDto resultado = await _administrarBonos.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _administrarBonos.EliminarAsync(id);
        return NoContent();
    }
}
