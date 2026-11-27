using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;
using TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Queries;

namespace TitanGlobal.Api.Controllers.Modulo08_ComisionesVendedores;

[ApiController]
[Route("api/v1/comisiones-vendedores")]
public class ComisionesVendedoresController : ControllerBase
{
    private readonly ISender _sender;

    public ComisionesVendedoresController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("vendedores")]
    public async Task<IActionResult> ListarVendedores(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaVendedoresDto resultado = await _sender.Send(new ListarVendedoresQuery
        {
            Busqueda = busqueda,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("vendedores/{id:int}")]
    public async Task<IActionResult> ObtenerVendedor(int id)
    {
        VendedorDetalleDto resultado = await _sender.Send(new ObtenerVendedorQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost("vendedores")]
    public async Task<IActionResult> CrearVendedor([FromBody] GuardarVendedorRequestDto solicitud)
    {
        IdDto resultado = await _sender.Send(new CrearVendedorCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPut("vendedores/{id:int}")]
    public async Task<IActionResult> ActualizarVendedor(int id, [FromBody] GuardarVendedorRequestDto solicitud)
    {
        IdDto resultado = await _sender.Send(new ActualizarVendedorCommand { Id = id, Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpDelete("vendedores/{id:int}")]
    public async Task<IActionResult> EliminarVendedor(int id)
    {
        await _sender.Send(new EliminarVendedorCommand { Id = id });
        return NoContent();
    }

    [HttpGet("ordenes")]
    public async Task<IActionResult> ListarOrdenes(
        [FromQuery] int? vendedorId,
        [FromQuery] bool? sinAsignar,
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaOrdenesVendedorDto resultado = await _sender.Send(new ListarOrdenesQuery
        {
            VendedorId = vendedorId,
            SinAsignar = sinAsignar,
            Anio = anio,
            Mes = mes,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpPut("ordenes/{ordenCompraId:int}/vendedor")]
    public async Task<IActionResult> AsignarVendedor(int ordenCompraId, [FromBody] AsignarVendedorRequestDto solicitud)
    {
        OrdenVendedorDto resultado = await _sender.Send(new AsignarVendedorCommand
        {
            OrdenCompraId = ordenCompraId,
            VendedorId = solicitud.VendedorId
        });
        return Ok(resultado);
    }

    [HttpDelete("ordenes/{ordenCompraId:int}/vendedor")]
    public async Task<IActionResult> QuitarAsignacion(int ordenCompraId)
    {
        await _sender.Send(new QuitarAsignacionCommand { OrdenCompraId = ordenCompraId });
        return NoContent();
    }

    [HttpGet("comisiones")]
    public async Task<IActionResult> ObtenerComisiones([FromQuery] int anio, [FromQuery] int mes)
    {
        ComisionesMesDto resultado = await _sender.Send(new ObtenerComisionesQuery { Anio = anio, Mes = mes });
        return Ok(resultado);
    }

    [HttpPost("liquidaciones")]
    public async Task<IActionResult> LiquidarMes([FromBody] LiquidarMesRequestDto solicitud)
    {
        ComisionesMesDto resultado = await _sender.Send(new LiquidarMesCommand
        {
            Anio = solicitud.Anio,
            Mes = solicitud.Mes
        });
        return Ok(resultado);
    }

    [HttpPost("liquidaciones/{id:int}/pagar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> PagarLiquidacion(int id)
    {
        LiquidacionDto resultado = await _sender.Send(new PagarLiquidacionCommand { Id = id });
        return Ok(resultado);
    }
}
