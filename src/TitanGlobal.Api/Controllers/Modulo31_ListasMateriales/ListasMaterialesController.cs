using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Application.UseCases.Modulo31_ListasMateriales.Commands;
using TitanGlobal.Application.UseCases.Modulo31_ListasMateriales.Queries;

namespace TitanGlobal.Api.Controllers.Modulo31_ListasMateriales;

[ApiController]
[Route("api/v1/listas-materiales")]
public class ListasMaterialesController : ControllerBase
{
    private readonly ISender _sender;

    public ListasMaterialesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaListasMaterialesDto resultado = await _sender.Send(new ListarListasMaterialesQuery
        {
            ProductoId = productoId,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ListaMaterialDetalleDto resultado = await _sender.Send(new ObtenerListaMaterialQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearListaMaterialRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearListaMaterialCommand { Solicitud = solicitud });
        return CreatedAtAction(nameof(Obtener), new { id }, new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarListaMaterialRequestDto solicitud)
    {
        int resultado = await _sender.Send(new EditarListaMaterialCommand { Id = id, Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/activar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        int resultado = await _sender.Send(new ActivarListaMaterialCommand { Id = id });
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/retirar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Retirar(int id)
    {
        int resultado = await _sender.Send(new RetirarListaMaterialCommand { Id = id });
        return Ok(new { Id = resultado });
    }
}
