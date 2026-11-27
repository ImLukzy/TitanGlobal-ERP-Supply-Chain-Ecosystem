using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Commands;
using TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Queries;

namespace TitanGlobal.Api.Controllers.Modulo02_SegmentacionClientes;

[ApiController]
[Route("api/v1/segmentos-clientes")]
public class SegmentosClientesController : ControllerBase
{
    private readonly ISender _sender;

    public SegmentosClientesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaSegmentosDto resultado = await _sender.Send(new ListarSegmentosClientesQuery
        {
            Busqueda = busqueda,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("clasificacion")]
    public async Task<IActionResult> Clasificacion(
        [FromQuery] int? segmentoId,
        [FromQuery] bool? sinSegmento,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaClientesSegmentoDto resultado = await _sender.Send(new ListarClientesClasificadosQuery
        {
            SegmentoId = segmentoId,
            SinSegmento = sinSegmento,
            Busqueda = busqueda,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenSegmentacionDto resultado = await _sender.Send(new ResumenSegmentacionClientesQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}/clientes")]
    public async Task<IActionResult> Clientes(int id, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaClientesSegmentoDto resultado = await _sender.Send(new ListarClientesDeSegmentoQuery
        {
            Id = id,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        SegmentoDetalleDto resultado = await _sender.Send(new ObtenerSegmentoClienteQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarSegmentoRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearSegmentoClienteCommand { Solicitud = solicitud });
        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarSegmentoRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarSegmentoClienteCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarSegmentoClienteCommand { Id = id });
        return NoContent();
    }
}
