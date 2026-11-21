using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

namespace TitanGlobal.Api.Controllers.Modulo46_MonitoreoEntregas;

[ApiController]
[Route("api/v1/monitoreo-entregas")]
public class MonitoreoEntregasController : ControllerBase
{
    private readonly ListarEntregasUseCase _listarEntregas;

    private readonly ObtenerEntregaUseCase _obtenerEntrega;

    private readonly CrearEntregaUseCase _crearEntrega;

    private readonly ActualizarEntregaUseCase _actualizarEntrega;

    private readonly EliminarEntregaUseCase _eliminarEntrega;

    private readonly IniciarEntregaUseCase _iniciarEntrega;

    private readonly CompletarEntregaUseCase _completarEntrega;

    private readonly CancelarEntregaUseCase _cancelarEntrega;

    private readonly ResumenEntregasUseCase _resumenEntregas;

    public MonitoreoEntregasController(
        ListarEntregasUseCase listarEntregas,
        ObtenerEntregaUseCase obtenerEntrega,
        CrearEntregaUseCase crearEntrega,
        ActualizarEntregaUseCase actualizarEntrega,
        EliminarEntregaUseCase eliminarEntrega,
        IniciarEntregaUseCase iniciarEntrega,
        CompletarEntregaUseCase completarEntrega,
        CancelarEntregaUseCase cancelarEntrega,
        ResumenEntregasUseCase resumenEntregas)
    {
        _listarEntregas = listarEntregas;
        _obtenerEntrega = obtenerEntrega;
        _crearEntrega = crearEntrega;
        _actualizarEntrega = actualizarEntrega;
        _eliminarEntrega = eliminarEntrega;
        _iniciarEntrega = iniciarEntrega;
        _completarEntrega = completarEntrega;
        _cancelarEntrega = cancelarEntrega;
        _resumenEntregas = resumenEntregas;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listarEntregas.ExecuteAsync(busqueda, estado, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _obtenerEntrega.ExecuteAsync(id));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        return Ok(await _resumenEntregas.ExecuteAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarEntregaRequestDto solicitud)
    {
        return Ok(new { Id = await _crearEntrega.ExecuteAsync(solicitud) });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarEntregaRequestDto solicitud)
    {
        return Ok(new { Id = await _actualizarEntrega.ExecuteAsync(id, solicitud) });
    }

    [HttpPost("{id:int}/salida")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Iniciar(int id)
    {
        return Ok(new { Id = await _iniciarEntrega.ExecuteAsync(id) });
    }

    [HttpPost("{id:int}/entrega")]
    public async Task<IActionResult> Completar(int id, [FromBody] RegistrarPodRequestDto solicitud)
    {
        return Ok(new { Id = await _completarEntrega.ExecuteAsync(id, solicitud) });
    }

    [HttpPost("{id:int}/cancelacion")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarEntregaRequestDto solicitud)
    {
        return Ok(new { Id = await _cancelarEntrega.ExecuteAsync(id, solicitud) });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarEntrega.ExecuteAsync(id);
        return NoContent();
    }
}
