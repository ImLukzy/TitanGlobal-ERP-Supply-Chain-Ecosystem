using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Commands;
using TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Queries;

namespace TitanGlobal.Api.Controllers.Modulo03_Cotizaciones;

[ApiController]
[Route("api/v1/cotizaciones")]
public class CotizacionesController : ControllerBase
{
    private readonly ISender _sender;

    public CotizacionesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? clienteId,
        [FromQuery] bool? vencidas,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaCotizacionesDto resultado = await _sender.Send(new ListarCotizacionesQuery
        {
            Busqueda = busqueda,
            Estado = estado,
            ClienteId = clienteId,
            Vencidas = vencidas,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenCotizacionesDto resultado = await _sender.Send(new ResumenCotizacionesQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CotizacionDetalleDto resultado = await _sender.Send(new ObtenerCotizacionQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarCotizacionRequestDto solicitud)
    {
        CotizacionCreadaDto resultado = await _sender.Send(new CrearCotizacionCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCotizacionRequestDto solicitud)
    {
        CotizacionCreadaDto resultado = await _sender.Send(new ActualizarCotizacionCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/aceptar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Aceptar(int id)
    {
        CotizacionDetalleDto resultado = await _sender.Send(new AceptarCotizacionCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarCotizacionRequestDto solicitud)
    {
        CotizacionDetalleDto resultado = await _sender.Send(new RechazarCotizacionCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarCotizacionCommand { Id = id });
        return NoContent();
    }
}
