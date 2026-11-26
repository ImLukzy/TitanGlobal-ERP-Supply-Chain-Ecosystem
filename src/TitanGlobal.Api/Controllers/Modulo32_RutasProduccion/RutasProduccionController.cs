using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

namespace TitanGlobal.Api.Controllers.Modulo32_RutasProduccion;

[ApiController]
[Route("api/v1/rutas-produccion")]
public class RutasProduccionController : ControllerBase
{
    private readonly ListarRutasProduccionUseCase _listar;
    private readonly ObtenerRutaProduccionUseCase _obtener;
    private readonly CrearRutaProduccionUseCase _crear;
    private readonly EditarRutaProduccionUseCase _editar;
    private readonly ActivarRutaProduccionUseCase _activar;
    private readonly RetirarRutaProduccionUseCase _retirar;

    public RutasProduccionController(
        ListarRutasProduccionUseCase listar,
        ObtenerRutaProduccionUseCase obtener,
        CrearRutaProduccionUseCase crear,
        EditarRutaProduccionUseCase editar,
        ActivarRutaProduccionUseCase activar,
        RetirarRutaProduccionUseCase retirar)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _editar = editar;
        _activar = activar;
        _retirar = retirar;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaRutasProduccionDto resultado = await _listar.ExecuteAsync(productoId, estado, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        RutaProduccionDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearRutaProduccionRequestDto solicitud)
    {
        int id = await _crear.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id }, new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarRutaProduccionRequestDto solicitud)
    {
        int resultado = await _editar.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/activar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        int resultado = await _activar.ExecuteAsync(id);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/retirar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Retirar(int id)
    {
        int resultado = await _retirar.ExecuteAsync(id);
        return Ok(new { Id = resultado });
    }
}
