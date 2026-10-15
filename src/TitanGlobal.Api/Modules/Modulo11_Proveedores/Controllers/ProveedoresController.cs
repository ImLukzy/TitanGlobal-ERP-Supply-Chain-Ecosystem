using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo11_Proveedores.Dtos;
using TitanGlobal.Api.Modules.Modulo11_Proveedores.UseCases;

namespace TitanGlobal.Api.Modules.Modulo11_Proveedores.Controllers;

[ApiController]
[Route("api/v1/proveedores")]
public class ProveedoresController : ControllerBase
{
    private readonly ListarProveedoresUseCase _listarProveedoresUseCase;

    private readonly ObtenerProveedorUseCase _obtenerProveedorUseCase;

    private readonly CrearProveedorUseCase _crearProveedorUseCase;

    private readonly ActualizarProveedorUseCase _actualizarProveedorUseCase;

    private readonly EliminarProveedorUseCase _eliminarProveedorUseCase;

    private readonly ListarPaisesProveedorUseCase _listarPaisesProveedorUseCase;

    private readonly ResumenProveedoresUseCase _resumenProveedoresUseCase;

    public ProveedoresController(
        ListarProveedoresUseCase listarProveedoresUseCase,
        ObtenerProveedorUseCase obtenerProveedorUseCase,
        CrearProveedorUseCase crearProveedorUseCase,
        ActualizarProveedorUseCase actualizarProveedorUseCase,
        EliminarProveedorUseCase eliminarProveedorUseCase,
        ListarPaisesProveedorUseCase listarPaisesProveedorUseCase,
        ResumenProveedoresUseCase resumenProveedoresUseCase)
    {
        _listarProveedoresUseCase = listarProveedoresUseCase;
        _obtenerProveedorUseCase = obtenerProveedorUseCase;
        _crearProveedorUseCase = crearProveedorUseCase;
        _actualizarProveedorUseCase = actualizarProveedorUseCase;
        _eliminarProveedorUseCase = eliminarProveedorUseCase;
        _listarPaisesProveedorUseCase = listarPaisesProveedorUseCase;
        _resumenProveedoresUseCase = resumenProveedoresUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? pais,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaProveedoresDto resultado = await _listarProveedoresUseCase.ExecuteAsync(
            busqueda,
            pais,
            activo,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("paises")]
    public async Task<IActionResult> Paises()
    {
        List<string> resultado = await _listarPaisesProveedorUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenProveedoresDto resultado = await _resumenProveedoresUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ProveedorDetalleDto resultado = await _obtenerProveedorUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarProveedorRequestDto solicitud)
    {
        int id = await _crearProveedorUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarProveedorRequestDto solicitud)
    {
        int idActualizado = await _actualizarProveedorUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarProveedorUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
