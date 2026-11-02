using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Application.UseCases.Modulo06_ListasPrecios;

namespace TitanGlobal.Api.Controllers.Modulo06_ListasPrecios;

[ApiController]
[Route("api/v1/listas-precios")]
public class ListasPreciosController : ControllerBase
{
    private readonly ListasPreciosUseCase _listasPreciosUseCase;

    public ListasPreciosController(ListasPreciosUseCase listasPreciosUseCase)
    {
        _listasPreciosUseCase = listasPreciosUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? segmentoId,
        [FromQuery] bool? vigentes,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaListasDto resultado = await _listasPreciosUseCase.ListarAsync(busqueda, segmentoId, vigentes, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenListasDto resultado = await _listasPreciosUseCase.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("precio")]
    public async Task<IActionResult> ResolverPrecio(
        [FromQuery] int productoId,
        [FromQuery] int clienteId,
        [FromQuery] DateTime? fecha)
    {
        PrecioResueltoDto resultado = await _listasPreciosUseCase.ResolverAsync(productoId, clienteId, fecha);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ListaDetalleDto resultado = await _listasPreciosUseCase.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarListaRequestDto solicitud)
    {
        int id = await _listasPreciosUseCase.GuardarAsync(solicitud, null);
        return Ok(new ListaCreadaDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarListaRequestDto solicitud)
    {
        int idActualizado = await _listasPreciosUseCase.GuardarAsync(solicitud, id);
        return Ok(new ListaCreadaDto { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _listasPreciosUseCase.EliminarAsync(id);
        return NoContent();
    }
}
