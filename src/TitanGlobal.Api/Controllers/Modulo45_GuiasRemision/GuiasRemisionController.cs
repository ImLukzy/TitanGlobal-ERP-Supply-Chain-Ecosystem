using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

namespace TitanGlobal.Api.Controllers.Modulo45_GuiasRemision;

[ApiController]
[Route("api/v1/guias-remision")]
public class GuiasRemisionController : ControllerBase
{
    private readonly ListarGuiasUseCase _listarGuias;

    private readonly ObtenerGuiaUseCase _obtenerGuia;

    private readonly CrearGuiaUseCase _crearGuia;

    private readonly ActualizarGuiaUseCase _actualizarGuia;

    private readonly EliminarGuiaUseCase _eliminarGuia;

    private readonly EmitirGuiaUseCase _emitirGuia;

    private readonly AnularGuiaUseCase _anularGuia;

    private readonly ResumenGuiasUseCase _resumenGuias;

    public GuiasRemisionController(
        ListarGuiasUseCase listarGuias,
        ObtenerGuiaUseCase obtenerGuia,
        CrearGuiaUseCase crearGuia,
        ActualizarGuiaUseCase actualizarGuia,
        EliminarGuiaUseCase eliminarGuia,
        EmitirGuiaUseCase emitirGuia,
        AnularGuiaUseCase anularGuia,
        ResumenGuiasUseCase resumenGuias)
    {
        _listarGuias = listarGuias;
        _obtenerGuia = obtenerGuia;
        _crearGuia = crearGuia;
        _actualizarGuia = actualizarGuia;
        _eliminarGuia = eliminarGuia;
        _emitirGuia = emitirGuia;
        _anularGuia = anularGuia;
        _resumenGuias = resumenGuias;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listarGuias.ExecuteAsync(busqueda, estado, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _obtenerGuia.ExecuteAsync(id));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        return Ok(await _resumenGuias.ExecuteAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarGuiaRequestDto solicitud)
    {
        return Ok(new { Id = await _crearGuia.ExecuteAsync(solicitud) });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarGuiaRequestDto solicitud)
    {
        return Ok(new { Id = await _actualizarGuia.ExecuteAsync(id, solicitud) });
    }

    [HttpPost("{id:int}/emision")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Emitir(int id)
    {
        return Ok(new { Id = await _emitirGuia.ExecuteAsync(id) });
    }

    [HttpPost("{id:int}/anulacion")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularGuiaRequestDto solicitud)
    {
        return Ok(new { Id = await _anularGuia.ExecuteAsync(id, solicitud) });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarGuia.ExecuteAsync(id);
        return NoContent();
    }
}
