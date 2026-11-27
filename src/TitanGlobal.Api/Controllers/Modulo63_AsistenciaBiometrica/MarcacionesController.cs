using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Commands;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Queries;

namespace TitanGlobal.Api.Controllers.Modulo63_AsistenciaBiometrica;

[ApiController]
[Route("api/v1/marcaciones")]
public class MarcacionesController : ControllerBase
{
    private readonly ISender _sender;

    public MarcacionesController(ISender sender)
    {
        _sender = sender;
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
        PaginaMarcacionesDto resultado = await _sender.Send(new ListarMarcacionesQuery
        {
            EmpleadoId = empleadoId,
            Desde = desde,
            Hasta = hasta,
            Tipo = tipo,
            Origen = origen,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarMarcacionRequestDto solicitud)
    {
        MarcacionIdDto resultado = await _sender.Send(new RegistrarMarcacionCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPost("importacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Importar([FromBody] ImportarMarcacionesRequestDto solicitud)
    {
        ResultadoImportacionDto resultado = await _sender.Send(new ImportarMarcacionesCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarMarcacionCommand { Id = id });
        return NoContent();
    }
}
