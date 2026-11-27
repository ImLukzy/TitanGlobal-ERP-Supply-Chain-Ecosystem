using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Application.UseCases.Modulo11_Proveedores.Commands;
using TitanGlobal.Application.UseCases.Modulo11_Proveedores.Queries;

namespace TitanGlobal.Api.Controllers.Modulo11_Proveedores;

[ApiController]
[Route("api/v1/proveedores")]
public class ProveedoresController : ControllerBase
{
    private readonly ISender _sender;

    public ProveedoresController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? pais,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaProveedoresDto resultado = await _sender.Send(new ListarProveedoresQuery
        {
            Busqueda = busqueda,
            Pais = pais,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("paises")]
    public async Task<IActionResult> Paises()
    {
        List<string> resultado = await _sender.Send(new ListarPaisesProveedorQuery());

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenProveedoresDto resultado = await _sender.Send(new ResumenProveedoresQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ProveedorDetalleDto resultado = await _sender.Send(new ObtenerProveedorQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarProveedorRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearProveedorCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarProveedorRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarProveedorCommand { Id = id, Solicitud = solicitud });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarProveedorCommand { Id = id });

        return NoContent();
    }
}
