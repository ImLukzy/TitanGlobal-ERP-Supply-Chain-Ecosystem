using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores;

namespace TitanGlobal.Api.Controllers.Modulo08_ComisionesVendedores;

[ApiController]
[Route("api/v1/comisiones-vendedores")]
public class ComisionesVendedoresController : ControllerBase
{
    private readonly ComisionesVendedoresUseCase _casoDeUso;

    public ComisionesVendedoresController(ComisionesVendedoresUseCase casoDeUso)
    {
        _casoDeUso = casoDeUso;
    }

    [HttpGet("vendedores")]
    public async Task<IActionResult> ListarVendedores(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaVendedoresDto resultado = await _casoDeUso.ListarVendedoresAsync(busqueda, activo, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("vendedores/{id:int}")]
    public async Task<IActionResult> ObtenerVendedor(int id)
    {
        VendedorDetalleDto resultado = await _casoDeUso.ObtenerVendedorAsync(id);
        return Ok(resultado);
    }

    [HttpPost("vendedores")]
    public async Task<IActionResult> CrearVendedor([FromBody] GuardarVendedorRequestDto solicitud)
    {
        IdDto resultado = await _casoDeUso.CrearVendedorAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPut("vendedores/{id:int}")]
    public async Task<IActionResult> ActualizarVendedor(int id, [FromBody] GuardarVendedorRequestDto solicitud)
    {
        IdDto resultado = await _casoDeUso.ActualizarVendedorAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpDelete("vendedores/{id:int}")]
    public async Task<IActionResult> EliminarVendedor(int id)
    {
        await _casoDeUso.EliminarVendedorAsync(id);
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
        PaginaOrdenesVendedorDto resultado = await _casoDeUso.ListarOrdenesAsync(
            vendedorId, sinAsignar, anio, mes, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPut("ordenes/{ordenCompraId:int}/vendedor")]
    public async Task<IActionResult> AsignarVendedor(int ordenCompraId, [FromBody] AsignarVendedorRequestDto solicitud)
    {
        OrdenVendedorDto resultado = await _casoDeUso.AsignarVendedorAsync(ordenCompraId, solicitud.VendedorId);
        return Ok(resultado);
    }

    [HttpDelete("ordenes/{ordenCompraId:int}/vendedor")]
    public async Task<IActionResult> QuitarAsignacion(int ordenCompraId)
    {
        await _casoDeUso.QuitarAsignacionAsync(ordenCompraId);
        return NoContent();
    }

    [HttpGet("comisiones")]
    public async Task<IActionResult> ObtenerComisiones([FromQuery] int anio, [FromQuery] int mes)
    {
        ComisionesMesDto resultado = await _casoDeUso.ObtenerComisionesAsync(anio, mes);
        return Ok(resultado);
    }

    [HttpPost("liquidaciones")]
    public async Task<IActionResult> LiquidarMes([FromBody] LiquidarMesRequestDto solicitud)
    {
        ComisionesMesDto resultado = await _casoDeUso.LiquidarMesAsync(solicitud.Anio, solicitud.Mes);
        return Ok(resultado);
    }

    [HttpPost("liquidaciones/{id:int}/pagar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> PagarLiquidacion(int id)
    {
        LiquidacionDto resultado = await _casoDeUso.PagarLiquidacionAsync(id);
        return Ok(resultado);
    }
}
