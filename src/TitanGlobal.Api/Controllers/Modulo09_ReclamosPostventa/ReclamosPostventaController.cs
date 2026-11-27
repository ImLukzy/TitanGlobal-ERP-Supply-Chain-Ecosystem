using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Commands;
using TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Queries;

namespace TitanGlobal.Api.Controllers.Modulo09_ReclamosPostventa;

[ApiController]
[Route("api/v1/reclamos-postventa")]
public class ReclamosPostventaController : ControllerBase
{
    private readonly ISender _sender;

    public ReclamosPostventaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? tipo,
        [FromQuery] string? prioridad,
        [FromQuery] int? clienteId,
        [FromQuery] bool? vencidos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaReclamosDto resultado = await _sender.Send(new ListarReclamosPostventaQuery
        {
            Busqueda = busqueda,
            Estado = estado,
            Tipo = tipo,
            Prioridad = prioridad,
            ClienteId = clienteId,
            Vencidos = vencidos,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenReclamosDto resultado = await _sender.Send(new ResumenReclamosPostventaQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ReclamoDetalleDto resultado = await _sender.Send(new ObtenerReclamoPostventaQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarReclamoRequestDto solicitud)
    {
        ReclamoCreadoDto resultado = await _sender.Send(new CrearReclamoPostventaCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarReclamoRequestDto solicitud)
    {
        ReclamoCreadoDto resultado = await _sender.Send(new ActualizarReclamoPostventaCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/iniciar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Iniciar(int id)
    {
        ReclamoDetalleDto resultado = await _sender.Send(new IniciarReclamoPostventaCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/resolver")]
    public async Task<IActionResult> Resolver(int id, [FromBody] ResolverReclamoRequestDto solicitud)
    {
        ReclamoDetalleDto resultado = await _sender.Send(new ResolverReclamoPostventaCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cerrar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cerrar(int id)
    {
        ReclamoDetalleDto resultado = await _sender.Send(new CerrarReclamoPostventaCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarReclamoRequestDto solicitud)
    {
        ReclamoDetalleDto resultado = await _sender.Send(new RechazarReclamoPostventaCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }
}
