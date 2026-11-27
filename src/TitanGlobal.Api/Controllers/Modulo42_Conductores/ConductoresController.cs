using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Application.UseCases.Modulo42_Conductores.Commands;
using TitanGlobal.Application.UseCases.Modulo42_Conductores.Queries;

namespace TitanGlobal.Api.Controllers.Modulo42_Conductores;

[ApiController]
[Route("api/v1/conductores")]
public class ConductoresController : ControllerBase
{
    private readonly ISender _sender;

    public ConductoresController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? habilitado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaConductoresDto resultado = await _sender.Send(new ListarConductoresQuery
        {
            Busqueda = busqueda,
            Habilitado = habilitado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ConductorDetalleDto resultado = await _sender.Send(new ObtenerConductorQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarConductorRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearConductorCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarConductorRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarConductorCommand { Id = id, Solicitud = solicitud });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarConductorCommand { Id = id });

        return NoContent();
    }
}
