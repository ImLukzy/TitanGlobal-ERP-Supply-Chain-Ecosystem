using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;
namespace TitanGlobal.Api.Controllers.Modulo50_SiniestrosFlota;

[ApiController]
[Route("api/v1/siniestros-flota")]
public class SiniestrosFlotaController : ControllerBase
{
    private readonly ListarSiniestrosUseCase _listar;
    private readonly ObtenerSiniestroUseCase _obtener;
    private readonly CrearSiniestroUseCase _crear;
    private readonly ActualizarSiniestroUseCase _actualizar;
    private readonly EliminarSiniestroUseCase _eliminar;
    private readonly ReclamarSiniestroUseCase _reclamar;
    private readonly CerrarSiniestroUseCase _cerrar;
    private readonly AnularSiniestroUseCase _anular;
    private readonly ResumenSiniestrosUseCase _resumen;
    public SiniestrosFlotaController(ListarSiniestrosUseCase listar, ObtenerSiniestroUseCase obtener, CrearSiniestroUseCase crear, ActualizarSiniestroUseCase actualizar, EliminarSiniestroUseCase eliminar, ReclamarSiniestroUseCase reclamar, CerrarSiniestroUseCase cerrar, AnularSiniestroUseCase anular, ResumenSiniestrosUseCase resumen)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _eliminar = eliminar;
        _reclamar = reclamar;
        _cerrar = cerrar;
        _anular = anular;
        _resumen = resumen;
    }
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] string? tipo, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listar.ExecuteAsync(busqueda, estado, tipo, pagina, tamanoPagina));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _obtener.ExecuteAsync(id));
    }
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarSiniestroRequestDto datos)
    {
        int resultado = await _crear.ExecuteAsync(datos);
        return Ok(new { id = resultado });
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarSiniestroRequestDto datos)
    {
        int resultado = await _actualizar.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminar.ExecuteAsync(id);
        return NoContent();
    }
    [HttpPost("{id:int}/reclamo")]
    public async Task<IActionResult> Reclamar(int id, [FromBody] RegistrarReclamoRequestDto datos)
    {
        int resultado = await _reclamar.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpPost("{id:int}/cierre")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] CerrarReclamoRequestDto datos)
    {
        int resultado = await _cerrar.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpPost("{id:int}/anulacion")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularSiniestroRequestDto datos)
    {
        int resultado = await _anular.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        return Ok(await _resumen.ExecuteAsync());
    }
}
