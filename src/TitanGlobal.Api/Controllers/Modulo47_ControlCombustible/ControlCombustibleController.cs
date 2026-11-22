using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

namespace TitanGlobal.Api.Controllers.Modulo47_ControlCombustible;

[ApiController]
[Route("api/v1/control-combustible")]
public class ControlCombustibleController : ControllerBase
{
    private readonly ListarValesUseCase _listarVales;

    private readonly ObtenerValeUseCase _obtenerVale;

    private readonly CrearValeUseCase _crearVale;

    private readonly ActualizarValeUseCase _actualizarVale;

    private readonly EliminarValeUseCase _eliminarVale;

    private readonly ConfirmarValeUseCase _confirmarVale;

    private readonly AnularValeUseCase _anularVale;

    private readonly ResumenValesUseCase _resumenVales;

    public ControlCombustibleController(
        ListarValesUseCase listarVales,
        ObtenerValeUseCase obtenerVale,
        CrearValeUseCase crearVale,
        ActualizarValeUseCase actualizarVale,
        EliminarValeUseCase eliminarVale,
        ConfirmarValeUseCase confirmarVale,
        AnularValeUseCase anularVale,
        ResumenValesUseCase resumenVales)
    {
        _listarVales = listarVales;
        _obtenerVale = obtenerVale;
        _crearVale = crearVale;
        _actualizarVale = actualizarVale;
        _eliminarVale = eliminarVale;
        _confirmarVale = confirmarVale;
        _anularVale = anularVale;
        _resumenVales = resumenVales;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] string? tipoCombustible, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listarVales.ExecuteAsync(busqueda, estado, tipoCombustible, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _obtenerVale.ExecuteAsync(id));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        return Ok(await _resumenVales.ExecuteAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarValeRequestDto solicitud)
    {
        return Ok(new { Id = await _crearVale.ExecuteAsync(solicitud) });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarValeRequestDto solicitud)
    {
        return Ok(new { Id = await _actualizarVale.ExecuteAsync(id, solicitud) });
    }

    [HttpPost("{id:int}/confirmacion")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Confirmar(int id)
    {
        return Ok(new { Id = await _confirmarVale.ExecuteAsync(id) });
    }

    [HttpPost("{id:int}/anulacion")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularValeRequestDto solicitud)
    {
        return Ok(new { Id = await _anularVale.ExecuteAsync(id, solicitud) });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarVale.ExecuteAsync(id);
        return NoContent();
    }
}
