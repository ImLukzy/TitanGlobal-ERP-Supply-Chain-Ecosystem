using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

namespace TitanGlobal.Api.Controllers.Modulo34_OrdenesProduccion;

[ApiController]
[Route("api/v1/ordenes-produccion")]
public class OrdenesProduccionController : ControllerBase
{
    private readonly ListarOrdenesProduccionUseCase _listar;
    private readonly ResumenOrdenesProduccionUseCase _resumen;
    private readonly ObtenerOrdenProduccionUseCase _obtener;
    private readonly CrearOrdenProduccionUseCase _crear;
    private readonly EditarOrdenProduccionUseCase _editar;

    public OrdenesProduccionController(
        ListarOrdenesProduccionUseCase listar,
        ResumenOrdenesProduccionUseCase resumen,
        ObtenerOrdenProduccionUseCase obtener,
        CrearOrdenProduccionUseCase crear,
        EditarOrdenProduccionUseCase editar)
    {
        _listar = listar;
        _resumen = resumen;
        _obtener = obtener;
        _crear = crear;
        _editar = editar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaOrdenesProduccionDto resultado =
            await _listar.ExecuteAsync(busqueda, productoId, estado, desde, hasta, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenOrdenesProduccionDto resultado = await _resumen.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        OrdenProduccionDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearOrdenProduccionRequestDto solicitud)
    {
        OrdenProduccionCreadaDto creada = await _crear.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarOrdenProduccionRequestDto solicitud)
    {
        int resultado = await _editar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }
}
