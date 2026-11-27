using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Application.UseCases.Modulo21_Productos.Commands;
using TitanGlobal.Application.UseCases.Modulo21_Productos.Queries;

namespace TitanGlobal.Api.Controllers.Modulo21_Productos;

[ApiController]
[Route("api/v1/productos")]
public class ProductosController : ControllerBase
{
    private readonly ISender _sender;

    public ProductosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? categoria,
        [FromQuery] decimal? precioMin,
        [FromQuery] decimal? precioMax,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaProductosDto resultado = await _sender.Send(new ListarProductosQuery
        {
            Busqueda = busqueda,
            Categoria = categoria,
            PrecioMin = precioMin,
            PrecioMax = precioMax,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias()
    {
        List<string> categorias = await _sender.Send(new ListarCategoriasProductoQuery());

        return Ok(categorias);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenProductosDto resultado = await _sender.Send(new ResumenProductosQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ProductoDetalleDto resultado = await _sender.Send(new ObtenerProductoQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarProductoRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearProductoCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarProductoRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarProductoCommand { Id = id, Solicitud = solicitud });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarProductoCommand { Id = id });

        return NoContent();
    }
}
