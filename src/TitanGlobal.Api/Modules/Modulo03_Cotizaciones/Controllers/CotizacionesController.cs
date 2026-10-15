using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;
using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Controllers;

[ApiController]
[Route("api/v1/cotizaciones")]
public class CotizacionesController : ControllerBase
{
    private readonly ListarCotizacionesUseCase _listarCotizacionesUseCase;
    private readonly ObtenerCotizacionUseCase _obtenerCotizacionUseCase;
    private readonly CrearCotizacionUseCase _crearCotizacionUseCase;
    private readonly ActualizarCotizacionUseCase _actualizarCotizacionUseCase;
    private readonly AceptarCotizacionUseCase _aceptarCotizacionUseCase;
    private readonly RechazarCotizacionUseCase _rechazarCotizacionUseCase;
    private readonly EliminarCotizacionUseCase _eliminarCotizacionUseCase;
    private readonly ResumenCotizacionesUseCase _resumenCotizacionesUseCase;

    public CotizacionesController(
        ListarCotizacionesUseCase listarCotizacionesUseCase,
        ObtenerCotizacionUseCase obtenerCotizacionUseCase,
        CrearCotizacionUseCase crearCotizacionUseCase,
        ActualizarCotizacionUseCase actualizarCotizacionUseCase,
        AceptarCotizacionUseCase aceptarCotizacionUseCase,
        RechazarCotizacionUseCase rechazarCotizacionUseCase,
        EliminarCotizacionUseCase eliminarCotizacionUseCase,
        ResumenCotizacionesUseCase resumenCotizacionesUseCase)
    {
        _listarCotizacionesUseCase = listarCotizacionesUseCase;
        _obtenerCotizacionUseCase = obtenerCotizacionUseCase;
        _crearCotizacionUseCase = crearCotizacionUseCase;
        _actualizarCotizacionUseCase = actualizarCotizacionUseCase;
        _aceptarCotizacionUseCase = aceptarCotizacionUseCase;
        _rechazarCotizacionUseCase = rechazarCotizacionUseCase;
        _eliminarCotizacionUseCase = eliminarCotizacionUseCase;
        _resumenCotizacionesUseCase = resumenCotizacionesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? clienteId,
        [FromQuery] bool? vencidas,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaCotizacionesDto resultado = await _listarCotizacionesUseCase.ExecuteAsync(
            busqueda,
            estado,
            clienteId,
            vencidas,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenCotizacionesDto resultado = await _resumenCotizacionesUseCase.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CotizacionDetalleDto resultado = await _obtenerCotizacionUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarCotizacionRequestDto solicitud)
    {
        CotizacionCreadaDto resultado = await _crearCotizacionUseCase.ExecuteAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCotizacionRequestDto solicitud)
    {
        CotizacionCreadaDto resultado = await _actualizarCotizacionUseCase.ExecuteAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/aceptar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Aceptar(int id)
    {
        CotizacionDetalleDto resultado = await _aceptarCotizacionUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarCotizacionRequestDto solicitud)
    {
        CotizacionDetalleDto resultado = await _rechazarCotizacionUseCase.ExecuteAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarCotizacionUseCase.ExecuteAsync(id);
        return NoContent();
    }
}
