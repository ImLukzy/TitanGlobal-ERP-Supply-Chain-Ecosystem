using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Application.UseCases.Modulo21_Productos;

namespace TitanGlobal.Api.Controllers.Modulo21_Productos;

[ApiController]
[Route("api/v1/productos")]
public class ProductosController : ControllerBase
{
    private readonly ListarProductosUseCase _listarProductosUseCase;

    private readonly ObtenerProductoUseCase _obtenerProductoUseCase;

    private readonly CrearProductoUseCase _crearProductoUseCase;

    private readonly ActualizarProductoUseCase _actualizarProductoUseCase;

    private readonly EliminarProductoUseCase _eliminarProductoUseCase;

    private readonly ListarCategoriasProductoUseCase _listarCategoriasProductoUseCase;

    private readonly ResumenProductosUseCase _resumenProductosUseCase;

    public ProductosController(
        ListarProductosUseCase listarProductosUseCase,
        ObtenerProductoUseCase obtenerProductoUseCase,
        CrearProductoUseCase crearProductoUseCase,
        ActualizarProductoUseCase actualizarProductoUseCase,
        EliminarProductoUseCase eliminarProductoUseCase,
        ListarCategoriasProductoUseCase listarCategoriasProductoUseCase,
        ResumenProductosUseCase resumenProductosUseCase)
    {
        _listarProductosUseCase = listarProductosUseCase;
        _obtenerProductoUseCase = obtenerProductoUseCase;
        _crearProductoUseCase = crearProductoUseCase;
        _actualizarProductoUseCase = actualizarProductoUseCase;
        _eliminarProductoUseCase = eliminarProductoUseCase;
        _listarCategoriasProductoUseCase = listarCategoriasProductoUseCase;
        _resumenProductosUseCase = resumenProductosUseCase;
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
        PaginaProductosDto resultado = await _listarProductosUseCase.ExecuteAsync(
            busqueda,
            categoria,
            precioMin,
            precioMax,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias()
    {
        List<string> categorias = await _listarCategoriasProductoUseCase.ExecuteAsync();

        return Ok(categorias);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenProductosDto resultado = await _resumenProductosUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ProductoDetalleDto resultado = await _obtenerProductoUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarProductoRequestDto solicitud)
    {
        int id = await _crearProductoUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarProductoRequestDto solicitud)
    {
        int idActualizado = await _actualizarProductoUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarProductoUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
