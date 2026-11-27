using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Commands;
using TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Queries;

namespace TitanGlobal.Api.Controllers.Modulo05_ContratosComerciales;

[ApiController]
[Route("api/v1/contratos-comerciales")]
public class ContratosComercialesController : ControllerBase
{
    private readonly ISender _sender;

    public ContratosComercialesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? clienteId,
        [FromQuery] bool? vencidos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaContratosDto resultado = await _sender.Send(new ListarContratosComercialesQuery
        {
            Busqueda = busqueda,
            Estado = estado,
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
        ResumenContratosDto resultado = await _sender.Send(new ResumenContratosComercialesQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ContratoDetalleDto resultado = await _sender.Send(new ObtenerContratoComercialQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarContratoRequestDto solicitud)
    {
        ContratoCreadoDto resultado = await _sender.Send(new CrearContratoComercialCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarContratoRequestDto solicitud)
    {
        ContratoCreadoDto resultado = await _sender.Send(new ActualizarContratoComercialCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/activar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        ContratoDetalleDto resultado = await _sender.Send(new ActivarContratoComercialCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/finalizar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Finalizar(int id)
    {
        ContratoDetalleDto resultado = await _sender.Send(new FinalizarContratoComercialCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarContratoRequestDto solicitud)
    {
        ContratoDetalleDto resultado = await _sender.Send(new CancelarContratoComercialCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarContratoComercialCommand { Id = id });
        return NoContent();
    }
}
