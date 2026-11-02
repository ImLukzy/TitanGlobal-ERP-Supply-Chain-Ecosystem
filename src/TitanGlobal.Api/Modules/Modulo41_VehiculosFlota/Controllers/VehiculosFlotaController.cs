using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.Dtos;
using TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.Controllers;

[ApiController]
[Route("api/v1/vehiculos-flota")]
public class VehiculosFlotaController : ControllerBase
{
    private readonly ListarVehiculosFlotaUseCase _listarVehiculosFlotaUseCase;

    private readonly ObtenerVehiculoFlotaUseCase _obtenerVehiculoFlotaUseCase;

    private readonly CrearVehiculoFlotaUseCase _crearVehiculoFlotaUseCase;

    private readonly ActualizarVehiculoFlotaUseCase _actualizarVehiculoFlotaUseCase;

    private readonly EliminarVehiculoFlotaUseCase _eliminarVehiculoFlotaUseCase;

    private readonly ListarTiposVehiculoFlotaUseCase _listarTiposVehiculoFlotaUseCase;

    private readonly ResumenVehiculosFlotaUseCase _resumenVehiculosFlotaUseCase;

    public VehiculosFlotaController(
        ListarVehiculosFlotaUseCase listarVehiculosFlotaUseCase,
        ObtenerVehiculoFlotaUseCase obtenerVehiculoFlotaUseCase,
        CrearVehiculoFlotaUseCase crearVehiculoFlotaUseCase,
        ActualizarVehiculoFlotaUseCase actualizarVehiculoFlotaUseCase,
        EliminarVehiculoFlotaUseCase eliminarVehiculoFlotaUseCase,
        ListarTiposVehiculoFlotaUseCase listarTiposVehiculoFlotaUseCase,
        ResumenVehiculosFlotaUseCase resumenVehiculosFlotaUseCase)
    {
        _listarVehiculosFlotaUseCase = listarVehiculosFlotaUseCase;
        _obtenerVehiculoFlotaUseCase = obtenerVehiculoFlotaUseCase;
        _crearVehiculoFlotaUseCase = crearVehiculoFlotaUseCase;
        _actualizarVehiculoFlotaUseCase = actualizarVehiculoFlotaUseCase;
        _eliminarVehiculoFlotaUseCase = eliminarVehiculoFlotaUseCase;
        _listarTiposVehiculoFlotaUseCase = listarTiposVehiculoFlotaUseCase;
        _resumenVehiculosFlotaUseCase = resumenVehiculosFlotaUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaVehiculosFlotaDto resultado = await _listarVehiculosFlotaUseCase.ExecuteAsync(
            busqueda,
            tipo,
            activo,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("tipos")]
    public async Task<IActionResult> Tipos()
    {
        List<string> resultado = await _listarTiposVehiculoFlotaUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenVehiculosFlotaDto resultado = await _resumenVehiculosFlotaUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        VehiculoFlotaDetalleDto resultado = await _obtenerVehiculoFlotaUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarVehiculoFlotaRequestDto solicitud)
    {
        int id = await _crearVehiculoFlotaUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarVehiculoFlotaRequestDto solicitud)
    {
        int idActualizado = await _actualizarVehiculoFlotaUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarVehiculoFlotaUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
