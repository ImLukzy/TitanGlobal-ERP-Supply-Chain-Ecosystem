using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

namespace TitanGlobal.Api.Controllers.Modulo22_CategoriasProducto;

[ApiController]
[Route("api/v1/categorias-producto")]
public class CategoriasProductoController : ControllerBase
{
    private readonly ListarCategoriasProductoUseCase _listarCategoriasProductoUseCase;

    private readonly ObtenerCategoriaProductoUseCase _obtenerCategoriaProductoUseCase;

    private readonly CrearCategoriaProductoUseCase _crearCategoriaProductoUseCase;

    private readonly ActualizarCategoriaProductoUseCase _actualizarCategoriaProductoUseCase;

    private readonly DesactivarCategoriaProductoUseCase _desactivarCategoriaProductoUseCase;

    private readonly ObtenerResumenCategoriasProductoUseCase _obtenerResumenCategoriasProductoUseCase;

    public CategoriasProductoController(
        ListarCategoriasProductoUseCase listarCategoriasProductoUseCase,
        ObtenerCategoriaProductoUseCase obtenerCategoriaProductoUseCase,
        CrearCategoriaProductoUseCase crearCategoriaProductoUseCase,
        ActualizarCategoriaProductoUseCase actualizarCategoriaProductoUseCase,
        DesactivarCategoriaProductoUseCase desactivarCategoriaProductoUseCase,
        ObtenerResumenCategoriasProductoUseCase obtenerResumenCategoriasProductoUseCase)
    {
        _listarCategoriasProductoUseCase = listarCategoriasProductoUseCase;
        _obtenerCategoriaProductoUseCase = obtenerCategoriaProductoUseCase;
        _crearCategoriaProductoUseCase = crearCategoriaProductoUseCase;
        _actualizarCategoriaProductoUseCase = actualizarCategoriaProductoUseCase;
        _desactivarCategoriaProductoUseCase = desactivarCategoriaProductoUseCase;
        _obtenerResumenCategoriasProductoUseCase = obtenerResumenCategoriasProductoUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar,
        [FromQuery] bool incluirInactivas = false)
    {
        List<CategoriaProductoDto> resultado = await _listarCategoriasProductoUseCase.ExecuteAsync(
            buscar,
            incluirInactivas);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenCategoriasProductoDto resultado = await _obtenerResumenCategoriasProductoUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CategoriaProductoDto resultado = await _obtenerCategoriaProductoUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCategoriaProductoDto solicitud)
    {
        int id = await _crearCategoriaProductoUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarCategoriaProductoDto solicitud)
    {
        int idActualizado = await _actualizarCategoriaProductoUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _desactivarCategoriaProductoUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
