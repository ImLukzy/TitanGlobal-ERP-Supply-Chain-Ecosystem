using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra.Commands;
using TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra.Queries;

namespace TitanGlobal.Api.Controllers.Modulo13_SolicitudesCompra;

[ApiController]
[Route("api/v1/solicitudes-compra")]
public class SolicitudesCompraController : ControllerBase
{
    private readonly ISender _sender;

    public SolicitudesCompraController(ISender sender)
    {
        _sender = sender;
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
        PaginaSolicitudesDto resultado = await _sender.Send(new ListarSolicitudesCompraQuery
        {
            Busqueda = busqueda,
            Estado = estado,
            ProveedorId = proveedorId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenSolicitudesDto resultado = await _sender.Send(new ResumenSolicitudesCompraQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        SolicitudDetalleDto resultado = await _sender.Send(new ObtenerSolicitudCompraQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarSolicitudRequestDto solicitud)
    {
        SolicitudCreadaDto resultado = await _sender.Send(new CrearSolicitudCompraCommand
        {
            Solicitud = solicitud
        });

        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarSolicitudRequestDto solicitud)
    {
        SolicitudCreadaDto resultado = await _sender.Send(new ActualizarSolicitudCompraCommand
        {
            Id = id,
            Solicitud = solicitud
        });

        return Ok(resultado);
    }

    [HttpPost("{id:int}/aprobar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Aprobar(int id)
    {
        SolicitudDetalleDto resultado = await _sender.Send(new AprobarSolicitudCompraCommand { Id = id });

        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarSolicitudRequestDto solicitudRechazo)
    {
        SolicitudDetalleDto resultado = await _sender.Send(new RechazarSolicitudCompraCommand
        {
            Id = id,
            SolicitudRechazo = solicitudRechazo
        });

        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarSolicitudCompraCommand { Id = id });

        return NoContent();
    }
}
