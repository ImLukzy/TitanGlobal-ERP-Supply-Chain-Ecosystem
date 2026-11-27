using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje.Commands;
using TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje.Queries;

namespace TitanGlobal.Api.Controllers.Modulo24_ZonasAlmacenaje;

[ApiController]
[Route("api/v1/zonas-almacenaje")]
public class ZonasAlmacenajeController : ControllerBase
{
    private readonly ISender _sender;

    public ZonasAlmacenajeController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? almacenId,
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] string? temperatura,
        [FromQuery] bool? activa,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaZonasDto resultado = await _sender.Send(new ListarZonasAlmacenajeQuery
        {
            AlmacenId = almacenId,
            Busqueda = busqueda,
            Tipo = tipo,
            Temperatura = temperatura,
            Activa = activa,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] int? almacenId)
    {
        ResumenZonasDto resultado = await _sender.Send(new ResumenZonasAlmacenajeQuery { AlmacenId = almacenId });

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ZonaDetalleDto resultado = await _sender.Send(new ObtenerZonaAlmacenajeQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarZonaRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearZonaAlmacenajeCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarZonaRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarZonaAlmacenajeCommand
        {
            Id = id,
            Solicitud = solicitud
        });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarZonaAlmacenajeCommand { Id = id });

        return NoContent();
    }
}
