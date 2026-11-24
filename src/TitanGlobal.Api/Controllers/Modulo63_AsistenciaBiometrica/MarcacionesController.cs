using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;

namespace TitanGlobal.Api.Controllers.Modulo63_AsistenciaBiometrica;

[ApiController]
[Route("api/v1/marcaciones")]
public class MarcacionesController : ControllerBase
{
    private readonly AdministrarMarcacionesUseCase _administrarMarcaciones;

    public MarcacionesController(AdministrarMarcacionesUseCase administrarMarcaciones)
    {
        _administrarMarcaciones = administrarMarcaciones;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] string? tipo,
        [FromQuery] string? origen,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaMarcacionesDto resultado = await _administrarMarcaciones.ListarAsync(
            empleadoId,
            desde,
            hasta,
            tipo,
            origen,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarMarcacionRequestDto solicitud)
    {
        MarcacionIdDto resultado = await _administrarMarcaciones.RegistrarAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("importacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Importar([FromBody] ImportarMarcacionesRequestDto solicitud)
    {
        ResultadoImportacionDto resultado = await _administrarMarcaciones.ImportarAsync(solicitud);
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _administrarMarcaciones.EliminarAsync(id);
        return NoContent();
    }
}
