using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
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
    private readonly LiberarOrdenProduccionUseCase _liberar;
    private readonly IniciarOrdenProduccionUseCase _iniciar;
    private readonly TerminarOrdenProduccionUseCase _terminar;
    private readonly CancelarOrdenProduccionUseCase _cancelar;

    public OrdenesProduccionController(
        ListarOrdenesProduccionUseCase listar,
        ResumenOrdenesProduccionUseCase resumen,
        ObtenerOrdenProduccionUseCase obtener,
        CrearOrdenProduccionUseCase crear,
        EditarOrdenProduccionUseCase editar,
        LiberarOrdenProduccionUseCase liberar,
        IniciarOrdenProduccionUseCase iniciar,
        TerminarOrdenProduccionUseCase terminar,
        CancelarOrdenProduccionUseCase cancelar)
    {
        _listar = listar;
        _resumen = resumen;
        _obtener = obtener;
        _crear = crear;
        _editar = editar;
        _liberar = liberar;
        _iniciar = iniciar;
        _terminar = terminar;
        _cancelar = cancelar;
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

    [HttpPost("{id:int}/liberar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Liberar(int id)
    {
        int resultado = await _liberar.ExecuteAsync(id);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/iniciar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Iniciar(int id)
    {
        int resultado = await _iniciar.ExecuteAsync(id);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/terminar")]
    public async Task<IActionResult> Terminar(int id, [FromBody] TerminarOrdenProduccionRequestDto solicitud)
    {
        int resultado = await _terminar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarOrdenProduccionRequestDto solicitud)
    {
        int resultado = await _cancelar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }
}
