using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa;

namespace TitanGlobal.Api.Controllers.Modulo09_ReclamosPostventa;

[ApiController]
[Route("api/v1/reclamos-postventa")]
public class ReclamosPostventaController : ControllerBase
{
    private readonly ReclamosPostventaUseCase _reclamosPostventaUseCase;

    public ReclamosPostventaController(ReclamosPostventaUseCase reclamosPostventaUseCase)
    {
        _reclamosPostventaUseCase = reclamosPostventaUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? tipo,
        [FromQuery] string? prioridad,
        [FromQuery] int? clienteId,
        [FromQuery] bool? vencidos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaReclamosDto resultado = await _reclamosPostventaUseCase.ListarAsync(
            busqueda,
            estado,
            tipo,
            prioridad,
            clienteId,
            vencidos,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenReclamosDto resultado = await _reclamosPostventaUseCase.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ReclamoDetalleDto resultado = await _reclamosPostventaUseCase.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarReclamoRequestDto solicitud)
    {
        ReclamoCreadoDto resultado = await _reclamosPostventaUseCase.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarReclamoRequestDto solicitud)
    {
        ReclamoCreadoDto resultado = await _reclamosPostventaUseCase.ActualizarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/iniciar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Iniciar(int id)
    {
        ReclamoDetalleDto resultado = await _reclamosPostventaUseCase.IniciarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/resolver")]
    public async Task<IActionResult> Resolver(int id, [FromBody] ResolverReclamoRequestDto solicitud)
    {
        ReclamoDetalleDto resultado = await _reclamosPostventaUseCase.ResolverAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cerrar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cerrar(int id)
    {
        ReclamoDetalleDto resultado = await _reclamosPostventaUseCase.CerrarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarReclamoRequestDto solicitud)
    {
        ReclamoDetalleDto resultado = await _reclamosPostventaUseCase.RechazarAsync(id, solicitud);
        return Ok(resultado);
    }
}
