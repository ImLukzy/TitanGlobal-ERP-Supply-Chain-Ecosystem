using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Application.UseCases.Modulo32_RutasProduccion.Commands;
using TitanGlobal.Application.UseCases.Modulo32_RutasProduccion.Queries;

namespace TitanGlobal.Api.Controllers.Modulo32_RutasProduccion;

[ApiController]
[Route("api/v1/rutas-produccion")]
public class RutasProduccionController : ControllerBase
{
    private readonly ISender _sender;

    public RutasProduccionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaRutasProduccionDto resultado = await _sender.Send(new ListarRutasProduccionQuery
        {
            ProductoId = productoId,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        RutaProduccionDetalleDto resultado = await _sender.Send(new ObtenerRutaProduccionQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearRutaProduccionRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearRutaProduccionCommand { Solicitud = solicitud });
        return CreatedAtAction(nameof(Obtener), new { id }, new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarRutaProduccionRequestDto solicitud)
    {
        int resultado = await _sender.Send(new EditarRutaProduccionCommand { Id = id, Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/activar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        int resultado = await _sender.Send(new ActivarRutaProduccionCommand { Id = id });
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/retirar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Retirar(int id)
    {
        int resultado = await _sender.Send(new RetirarRutaProduccionCommand { Id = id });
        return Ok(new { Id = resultado });
    }
}
