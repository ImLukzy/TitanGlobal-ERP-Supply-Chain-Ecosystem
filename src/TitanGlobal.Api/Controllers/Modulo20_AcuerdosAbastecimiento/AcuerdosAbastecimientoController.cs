using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

namespace TitanGlobal.Api.Controllers.Modulo20_AcuerdosAbastecimiento;

[ApiController]
[Route("api/v1/acuerdos-abastecimiento")]
public class AcuerdosAbastecimientoController : ControllerBase
{
    private readonly ListarAcuerdosUseCase _listarAcuerdosUseCase;

    private readonly ObtenerAcuerdoUseCase _obtenerAcuerdoUseCase;

    private readonly CrearAcuerdoUseCase _crearAcuerdoUseCase;

    private readonly ActualizarAcuerdoUseCase _actualizarAcuerdoUseCase;

    private readonly EliminarAcuerdoUseCase _eliminarAcuerdoUseCase;

    private readonly CrearLineaAcuerdoUseCase _crearLineaUseCase;

    private readonly EliminarLineaAcuerdoUseCase _eliminarLineaUseCase;

    private readonly RegistrarConsumoAcuerdoUseCase _registrarConsumoUseCase;

    private readonly SuspenderAcuerdoUseCase _suspenderAcuerdoUseCase;

    private readonly ReactivarAcuerdoUseCase _reactivarAcuerdoUseCase;

    private readonly CancelarAcuerdoUseCase _cancelarAcuerdoUseCase;

    private readonly ResumenAcuerdosUseCase _resumenAcuerdosUseCase;

    public AcuerdosAbastecimientoController(
        ListarAcuerdosUseCase listarAcuerdosUseCase,
        ObtenerAcuerdoUseCase obtenerAcuerdoUseCase,
        CrearAcuerdoUseCase crearAcuerdoUseCase,
        ActualizarAcuerdoUseCase actualizarAcuerdoUseCase,
        EliminarAcuerdoUseCase eliminarAcuerdoUseCase,
        CrearLineaAcuerdoUseCase crearLineaUseCase,
        EliminarLineaAcuerdoUseCase eliminarLineaUseCase,
        RegistrarConsumoAcuerdoUseCase registrarConsumoUseCase,
        SuspenderAcuerdoUseCase suspenderAcuerdoUseCase,
        ReactivarAcuerdoUseCase reactivarAcuerdoUseCase,
        CancelarAcuerdoUseCase cancelarAcuerdoUseCase,
        ResumenAcuerdosUseCase resumenAcuerdosUseCase)
    {
        _listarAcuerdosUseCase = listarAcuerdosUseCase;
        _obtenerAcuerdoUseCase = obtenerAcuerdoUseCase;
        _crearAcuerdoUseCase = crearAcuerdoUseCase;
        _actualizarAcuerdoUseCase = actualizarAcuerdoUseCase;
        _eliminarAcuerdoUseCase = eliminarAcuerdoUseCase;
        _crearLineaUseCase = crearLineaUseCase;
        _eliminarLineaUseCase = eliminarLineaUseCase;
        _registrarConsumoUseCase = registrarConsumoUseCase;
        _suspenderAcuerdoUseCase = suspenderAcuerdoUseCase;
        _reactivarAcuerdoUseCase = reactivarAcuerdoUseCase;
        _cancelarAcuerdoUseCase = cancelarAcuerdoUseCase;
        _resumenAcuerdosUseCase = resumenAcuerdosUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? proveedorId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAcuerdosDto resultado = await _listarAcuerdosUseCase.ExecuteAsync(
            busqueda,
            estado,
            proveedorId,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenAcuerdosDto resultado = await _resumenAcuerdosUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        AcuerdoDetalleDto resultado = await _obtenerAcuerdoUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearAcuerdoRequestDto solicitud)
    {
        int id = await _crearAcuerdoUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarAcuerdoRequestDto solicitud)
    {
        int idActualizado = await _actualizarAcuerdoUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarAcuerdoUseCase.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:int}/lineas")]
    public async Task<IActionResult> CrearLinea(int id, [FromBody] CrearLineaRequestDto solicitud)
    {
        int lineaId = await _crearLineaUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = lineaId });
    }

    [HttpDelete("{id:int}/lineas/{lineaId:int}")]
    public async Task<IActionResult> EliminarLinea(int id, int lineaId)
    {
        await _eliminarLineaUseCase.ExecuteAsync(id, lineaId);

        return NoContent();
    }

    [HttpPost("{id:int}/lineas/{lineaId:int}/consumos")]
    public async Task<IActionResult> RegistrarConsumo(
        int id,
        int lineaId,
        [FromBody] RegistrarConsumoRequestDto solicitud)
    {
        int idActualizado = await _registrarConsumoUseCase.ExecuteAsync(id, lineaId, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpPost("{id:int}/suspender")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Suspender(int id)
    {
        int idSuspendido = await _suspenderAcuerdoUseCase.ExecuteAsync(id);

        return Ok(new { Id = idSuspendido });
    }

    [HttpPost("{id:int}/reactivar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Reactivar(int id)
    {
        int idReactivado = await _reactivarAcuerdoUseCase.ExecuteAsync(id);

        return Ok(new { Id = idReactivado });
    }

    [HttpPost("{id:int}/cancelar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cancelar(int id)
    {
        int idCancelado = await _cancelarAcuerdoUseCase.ExecuteAsync(id);

        return Ok(new { Id = idCancelado });
    }
}
