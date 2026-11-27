using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Commands;
using TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Queries;

namespace TitanGlobal.Api.Controllers.Modulo15_OrdenesAbastecimiento;

[ApiController]
[Route("api/v1/ordenes-abastecimiento")]
public class OrdenesAbastecimientoController : ControllerBase
{
    private readonly ISender _sender;

    public OrdenesAbastecimientoController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaOrdenesAbastecimientoDto>> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? proveedorId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaOrdenesAbastecimientoDto resultado = await _sender.Send(new ListarOrdenesAbastecimientoQuery
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
    public async Task<ActionResult<ResumenOrdenesAbastecimientoDto>> Resumen()
    {
        ResumenOrdenesAbastecimientoDto resultado = await _sender.Send(new ResumenOrdenesAbastecimientoQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrdenAbastecimientoDetalleDto>> Obtener(int id)
    {
        OrdenAbastecimientoDetalleDto resultado = await _sender.Send(new ObtenerOrdenAbastecimientoQuery
        {
            Id = id
        });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<OrdenAbastecimientoCreadaDto>> Crear(
        [FromBody] GuardarOrdenAbastecimientoRequestDto solicitud)
    {
        OrdenAbastecimientoCreadaDto resultado = await _sender.Send(new CrearOrdenAbastecimientoCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("desde-solicitud/{solicitudId:int}")]
    public async Task<ActionResult<OrdenAbastecimientoCreadaDto>> GenerarDesdeSolicitud(
        int solicitudId,
        [FromBody] GenerarDesdeSolicitudRequestDto solicitud)
    {
        OrdenAbastecimientoCreadaDto resultado = await _sender.Send(new GenerarOrdenDesdeSolicitudCommand
        {
            SolicitudId = solicitudId,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrdenAbastecimientoCreadaDto>> Actualizar(
        int id,
        [FromBody] GuardarOrdenAbastecimientoRequestDto solicitud)
    {
        OrdenAbastecimientoCreadaDto resultado = await _sender.Send(new ActualizarOrdenAbastecimientoCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/recibir")]
    [PermitirCuerpoVacio]
    public async Task<ActionResult<OrdenAbastecimientoDetalleDto>> Recibir(int id)
    {
        OrdenAbastecimientoDetalleDto resultado = await _sender.Send(new RecibirOrdenAbastecimientoCommand
        {
            Id = id
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/anular")]
    public async Task<ActionResult<OrdenAbastecimientoDetalleDto>> Anular(
        int id,
        [FromBody] AnularOrdenRequestDto solicitud)
    {
        OrdenAbastecimientoDetalleDto resultado = await _sender.Send(new AnularOrdenAbastecimientoCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }
}
