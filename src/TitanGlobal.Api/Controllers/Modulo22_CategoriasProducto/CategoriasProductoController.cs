using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;
using TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Commands;
using TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto.Queries;

namespace TitanGlobal.Api.Controllers.Modulo22_CategoriasProducto;

[ApiController]
[Route("api/v1/categorias-producto")]
public class CategoriasProductoController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriasProductoController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar,
        [FromQuery] bool incluirInactivas = false)
    {
        List<CategoriaProductoDto> resultado = await _sender.Send(new ListarCategoriasProductoQuery
        {
            Buscar = buscar,
            IncluirInactivas = incluirInactivas
        });

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenCategoriasProductoDto resultado = await _sender.Send(new ObtenerResumenCategoriasProductoQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CategoriaProductoDto resultado = await _sender.Send(new ObtenerCategoriaProductoQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCategoriaProductoDto solicitud)
    {
        int id = await _sender.Send(new CrearCategoriaProductoCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarCategoriaProductoDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarCategoriaProductoCommand
        {
            Id = id,
            Solicitud = solicitud
        });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _sender.Send(new DesactivarCategoriaProductoCommand { Id = id });

        return NoContent();
    }
}
