using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Modules.Modulo10_DevolucionesComerciales.Dtos;
using TitanGlobal.Api.Modules.Modulo10_DevolucionesComerciales.UseCases;

namespace TitanGlobal.Api.Modules.Modulo10_DevolucionesComerciales.Controllers;

[ApiController]
[Route("api/v1/devoluciones-comerciales")]
public class DevolucionesComercialesController : ControllerBase
{
    private readonly DevolucionesComercialesUseCase _devolucionesComercialesUseCase;

    public DevolucionesComercialesController(DevolucionesComercialesUseCase devolucionesComercialesUseCase)
    {
        _devolucionesComercialesUseCase = devolucionesComercialesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? motivo,
        [FromQuery] int? clienteId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaDevolucionesDto resultado = await _devolucionesComercialesUseCase.ListarAsync(
            busqueda,
            estado,
            motivo,
            clienteId,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenDevolucionesDto resultado = await _devolucionesComercialesUseCase.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("ordenes/{ordenId:int}/disponible")]
    public async Task<IActionResult> ObtenerDisponibles(int ordenId)
    {
        List<ProductoDisponibleDto> resultado = await _devolucionesComercialesUseCase.ObtenerDisponiblesAsync(ordenId);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        DevolucionDetalleDto resultado = await _devolucionesComercialesUseCase.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarDevolucionRequestDto solicitud)
    {
        DevolucionCreadaDto resultado = await _devolucionesComercialesUseCase.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarDevolucionRequestDto solicitud)
    {
        DevolucionCreadaDto resultado = await _devolucionesComercialesUseCase.ActualizarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/aprobar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Aprobar(int id)
    {
        DevolucionDetalleDto resultado = await _devolucionesComercialesUseCase.AprobarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/recibir")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Recibir(int id)
    {
        DevolucionDetalleDto resultado = await _devolucionesComercialesUseCase.RecibirAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarDevolucionRequestDto solicitud)
    {
        DevolucionDetalleDto resultado = await _devolucionesComercialesUseCase.RechazarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _devolucionesComercialesUseCase.EliminarAsync(id);
        return NoContent();
    }
}
