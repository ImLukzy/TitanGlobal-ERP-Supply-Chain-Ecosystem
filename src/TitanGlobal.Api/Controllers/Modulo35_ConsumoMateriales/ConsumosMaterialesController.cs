using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;
using TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

namespace TitanGlobal.Api.Controllers.Modulo35_ConsumoMateriales;

[ApiController]
[Route("api/v1/consumos-materiales")]
public class ConsumosMaterialesController : ControllerBase
{
    private readonly ListarConsumosMaterialesUseCase _listar;
    private readonly ResumenConsumosMaterialesUseCase _resumen;
    private readonly BalanceConsumoOrdenUseCase _balance;
    private readonly ObtenerConsumoMaterialUseCase _obtener;
    private readonly RegistrarConsumoMaterialUseCase _registrar;

    public ConsumosMaterialesController(
        ListarConsumosMaterialesUseCase listar,
        ResumenConsumosMaterialesUseCase resumen,
        BalanceConsumoOrdenUseCase balance,
        ObtenerConsumoMaterialUseCase obtener,
        RegistrarConsumoMaterialUseCase registrar)
    {
        _listar = listar;
        _resumen = resumen;
        _balance = balance;
        _obtener = obtener;
        _registrar = registrar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? ordenProduccionId,
        [FromQuery] int? componenteProductoId,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaConsumosMaterialesDto resultado = await _listar.ExecuteAsync(
            busqueda, ordenProduccionId, componenteProductoId, estado, desde, hasta, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenConsumosMaterialesDto resultado = await _resumen.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("ordenes/{ordenProduccionId:int}/balance")]
    public async Task<IActionResult> Balance(int ordenProduccionId)
    {
        BalanceConsumoOrdenDto resultado = await _balance.ExecuteAsync(ordenProduccionId);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ConsumoMaterialDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarConsumoMaterialRequestDto solicitud)
    {
        ConsumoMaterialRegistradoDto registrado = await _registrar.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id = registrado.Id }, registrado);
    }
}
