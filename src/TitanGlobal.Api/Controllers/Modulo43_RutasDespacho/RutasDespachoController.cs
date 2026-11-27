using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Commands;
using TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Queries;

namespace TitanGlobal.Api.Controllers.Modulo43_RutasDespacho;

[ApiController]
[Route("api/v1/rutas-despacho")]
public class RutasDespachoController : ControllerBase
{
    private readonly ISender _sender;

    public RutasDespachoController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? zona, [FromQuery] bool? activo, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaRutasDespachoDto resultado = await _sender.Send(new ListarRutasDespachoQuery
        {
            Busqueda = busqueda,
            Zona = zona,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        RutaDespachoDetalleDto resultado = await _sender.Send(new ObtenerRutaDespachoQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarRutaDespachoRequestDto solicitud)
    {
        int resultado = await _sender.Send(new CrearRutaDespachoCommand { Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarRutaDespachoRequestDto solicitud)
    {
        int resultado = await _sender.Send(new ActualizarRutaDespachoCommand { Id = id, Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarRutaDespachoCommand { Id = id });
        return NoContent();
    }

    [HttpGet("zonas")]
    public async Task<IActionResult> Zonas()
    {
        List<string> resultado = await _sender.Send(new ListarZonasRutaDespachoQuery());
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenRutasDespachoDto resultado = await _sender.Send(new ResumenRutasDespachoQuery());
        return Ok(resultado);
    }
}
