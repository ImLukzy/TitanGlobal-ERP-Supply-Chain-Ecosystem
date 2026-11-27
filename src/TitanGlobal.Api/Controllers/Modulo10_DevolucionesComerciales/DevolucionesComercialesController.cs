using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Commands;
using TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Queries;

namespace TitanGlobal.Api.Controllers.Modulo10_DevolucionesComerciales;

[ApiController]
[Route("api/v1/devoluciones-comerciales")]
public class DevolucionesComercialesController : ControllerBase
{
    private readonly ISender _sender;

    public DevolucionesComercialesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? motivo,
        [FromQuery] int? clienteId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaDevolucionesDto resultado = await _sender.Send(new ListarDevolucionesComercialesQuery
        {
            Busqueda = busqueda,
            Estado = estado,
            Motivo = motivo,
            ClienteId = clienteId,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenDevolucionesDto resultado = await _sender.Send(new ResumenDevolucionesComercialesQuery());
        return Ok(resultado);
    }

    [HttpGet("ordenes/{ordenId:int}/disponible")]
    public async Task<IActionResult> ObtenerDisponibles(int ordenId)
    {
        List<ProductoDisponibleDto> resultado = await _sender.Send(new ObtenerDisponiblesQuery
        {
            OrdenId = ordenId
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        DevolucionDetalleDto resultado = await _sender.Send(new ObtenerDevolucionComercialQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarDevolucionRequestDto solicitud)
    {
        DevolucionCreadaDto resultado = await _sender.Send(new CrearDevolucionComercialCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarDevolucionRequestDto solicitud)
    {
        DevolucionCreadaDto resultado = await _sender.Send(new ActualizarDevolucionComercialCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/aprobar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Aprobar(int id)
    {
        DevolucionDetalleDto resultado = await _sender.Send(new AprobarDevolucionComercialCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/recibir")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Recibir(int id)
    {
        DevolucionDetalleDto resultado = await _sender.Send(new RecibirDevolucionComercialCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarDevolucionRequestDto solicitud)
    {
        DevolucionDetalleDto resultado = await _sender.Send(new RechazarDevolucionComercialCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarDevolucionComercialCommand { Id = id });
        return NoContent();
    }
}
