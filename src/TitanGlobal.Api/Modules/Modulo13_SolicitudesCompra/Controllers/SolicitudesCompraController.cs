using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;
using TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.UseCases;

namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Controllers;

[ApiController]
[Route("api/v1/solicitudes-compra")]
public class SolicitudesCompraController : ControllerBase
{
    private readonly ListarSolicitudesCompraUseCase _listarSolicitudesCompraUseCase;

    private readonly ObtenerSolicitudCompraUseCase _obtenerSolicitudCompraUseCase;

    private readonly CrearSolicitudCompraUseCase _crearSolicitudCompraUseCase;

    private readonly ActualizarSolicitudCompraUseCase _actualizarSolicitudCompraUseCase;

    private readonly AprobarSolicitudCompraUseCase _aprobarSolicitudCompraUseCase;

    private readonly RechazarSolicitudCompraUseCase _rechazarSolicitudCompraUseCase;

    private readonly EliminarSolicitudCompraUseCase _eliminarSolicitudCompraUseCase;

    private readonly ResumenSolicitudesCompraUseCase _resumenSolicitudesCompraUseCase;

    public SolicitudesCompraController(
        ListarSolicitudesCompraUseCase listarSolicitudesCompraUseCase,
        ObtenerSolicitudCompraUseCase obtenerSolicitudCompraUseCase,
        CrearSolicitudCompraUseCase crearSolicitudCompraUseCase,
        ActualizarSolicitudCompraUseCase actualizarSolicitudCompraUseCase,
        AprobarSolicitudCompraUseCase aprobarSolicitudCompraUseCase,
        RechazarSolicitudCompraUseCase rechazarSolicitudCompraUseCase,
        EliminarSolicitudCompraUseCase eliminarSolicitudCompraUseCase,
        ResumenSolicitudesCompraUseCase resumenSolicitudesCompraUseCase)
    {
        _listarSolicitudesCompraUseCase = listarSolicitudesCompraUseCase;
        _obtenerSolicitudCompraUseCase = obtenerSolicitudCompraUseCase;
        _crearSolicitudCompraUseCase = crearSolicitudCompraUseCase;
        _actualizarSolicitudCompraUseCase = actualizarSolicitudCompraUseCase;
        _aprobarSolicitudCompraUseCase = aprobarSolicitudCompraUseCase;
        _rechazarSolicitudCompraUseCase = rechazarSolicitudCompraUseCase;
        _eliminarSolicitudCompraUseCase = eliminarSolicitudCompraUseCase;
        _resumenSolicitudesCompraUseCase = resumenSolicitudesCompraUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? proveedorId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaSolicitudesDto resultado = await _listarSolicitudesCompraUseCase.ExecuteAsync(
            busqueda,
            estado,
            proveedorId,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenSolicitudesDto resultado = await _resumenSolicitudesCompraUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        SolicitudDetalleDto resultado = await _obtenerSolicitudCompraUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarSolicitudRequestDto solicitud)
    {
        SolicitudCreadaDto resultado = await _crearSolicitudCompraUseCase.ExecuteAsync(solicitud);

        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarSolicitudRequestDto solicitud)
    {
        SolicitudCreadaDto resultado = await _actualizarSolicitudCompraUseCase.ExecuteAsync(id, solicitud);

        return Ok(resultado);
    }

    [HttpPost("{id:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int id)
    {
        SolicitudDetalleDto resultado = await _aprobarSolicitudCompraUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarSolicitudRequestDto solicitudRechazo)
    {
        SolicitudDetalleDto resultado = await _rechazarSolicitudCompraUseCase.ExecuteAsync(id, solicitudRechazo);

        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarSolicitudCompraUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
