using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

namespace TitanGlobal.Api.Controllers.Modulo31_ListasMateriales;

[ApiController]
[Route("api/v1/listas-materiales")]
public class ListasMaterialesController : ControllerBase
{
    private readonly ListarListasMaterialesUseCase _listar;
    private readonly ObtenerListaMaterialUseCase _obtener;
    private readonly CrearListaMaterialUseCase _crear;
    private readonly EditarListaMaterialUseCase _editar;
    private readonly ActivarListaMaterialUseCase _activar;
    private readonly RetirarListaMaterialUseCase _retirar;

    public ListasMaterialesController(
        ListarListasMaterialesUseCase listar,
        ObtenerListaMaterialUseCase obtener,
        CrearListaMaterialUseCase crear,
        EditarListaMaterialUseCase editar,
        ActivarListaMaterialUseCase activar,
        RetirarListaMaterialUseCase retirar)
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
        PaginaListasMaterialesDto resultado = await _listar.ExecuteAsync(productoId, estado, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ListaMaterialDetalleDto resultado = await _obtener.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearListaMaterialRequestDto solicitud)
    {
        int id = await _crear.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id }, new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarListaMaterialRequestDto solicitud)
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
