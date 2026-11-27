using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Commands;
using TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Queries;

namespace TitanGlobal.Api.Controllers.Modulo44_ConsolidacionCarga;

[ApiController]
[Route("api/v1/consolidaciones-carga")]
public class ConsolidacionesCargaController : ControllerBase
{
    private readonly ISender _sender;
    public ConsolidacionesCargaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaConsolidacionesDto resultado = await _sender.Send(new ListarConsolidacionesQuery
        {
            Busqueda = busqueda,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ConsolidacionDetalleDto resultado = await _sender.Send(new ObtenerConsolidacionQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarConsolidacionRequestDto solicitud)
    {
        int resultado = await _sender.Send(new CrearConsolidacionCommand { Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarConsolidacionRequestDto solicitud)
    {
        int resultado = await _sender.Send(new ActualizarConsolidacionCommand { Id = id, Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarConsolidacionCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id:int}/confirmacion")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Confirmar(int id)
    {
        int resultado = await _sender.Send(new ConfirmarConsolidacionCommand { Id = id });
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/cancelacion")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarConsolidacionRequestDto solicitud)
    {
        int resultado = await _sender.Send(new CancelarConsolidacionCommand { Id = id, Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenConsolidacionesDto resultado = await _sender.Send(new ResumenConsolidacionesQuery());
        return Ok(resultado);
    }
}
