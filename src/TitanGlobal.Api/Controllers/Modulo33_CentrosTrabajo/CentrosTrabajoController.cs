using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Commands;
using TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Queries;

namespace TitanGlobal.Api.Controllers.Modulo33_CentrosTrabajo;

[ApiController]
[Route("api/v1/centros-trabajo")]
public class CentrosTrabajoController : ControllerBase
{
    private readonly ISender _sender;

    public CentrosTrabajoController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? planta,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaCentrosTrabajoDto resultado = await _sender.Send(new ListarCentrosTrabajoQuery
        {
            Busqueda = busqueda,
            Planta = planta,
            Tipo = tipo,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("plantas")]
    public async Task<IActionResult> Plantas()
    {
        List<string> resultado = await _sender.Send(new ListarPlantasCentroTrabajoQuery());

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenCentrosTrabajoDto resultado = await _sender.Send(new ResumenCentrosTrabajoQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CentroTrabajoDetalleDto resultado = await _sender.Send(new ObtenerCentroTrabajoQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarCentroTrabajoRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearCentroTrabajoCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCentroTrabajoRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarCentroTrabajoCommand
        {
            Id = id,
            Solicitud = solicitud
        });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarCentroTrabajoCommand { Id = id });

        return NoContent();
    }
}
