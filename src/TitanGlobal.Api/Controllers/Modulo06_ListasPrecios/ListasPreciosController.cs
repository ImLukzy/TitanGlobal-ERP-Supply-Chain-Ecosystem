using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Commands;
using TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Queries;

namespace TitanGlobal.Api.Controllers.Modulo06_ListasPrecios;

[ApiController]
[Route("api/v1/listas-precios")]
public class ListasPreciosController : ControllerBase
{
    private readonly ISender _sender;

    public ListasPreciosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? segmentoId,
        [FromQuery] bool? vigentes,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaListasDto resultado = await _sender.Send(new ListarListasPreciosQuery
        {
            Busqueda = busqueda,
            SegmentoId = segmentoId,
            Vigentes = vigentes,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenListasDto resultado = await _sender.Send(new ResumenListasPreciosQuery());
        return Ok(resultado);
    }

    [HttpGet("precio")]
    public async Task<IActionResult> ResolverPrecio(
        [FromQuery] int productoId,
        [FromQuery] int clienteId,
        [FromQuery] DateTime? fecha)
    {
        PrecioResueltoDto resultado = await _sender.Send(new ResolverPrecioQuery
        {
            ProductoId = productoId,
            ClienteId = clienteId,
            Fecha = fecha
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ListaDetalleDto resultado = await _sender.Send(new ObtenerListaPreciosQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarListaRequestDto solicitud)
    {
        int id = await _sender.Send(new GuardarListaPreciosCommand { Solicitud = solicitud, Id = null });
        return Ok(new ListaCreadaDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarListaRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new GuardarListaPreciosCommand { Solicitud = solicitud, Id = id });
        return Ok(new ListaCreadaDto { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarListaPreciosCommand { Id = id });
        return NoContent();
    }
}
