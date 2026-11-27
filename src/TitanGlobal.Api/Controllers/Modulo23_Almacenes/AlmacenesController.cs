using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Application.UseCases.Modulo23_Almacenes.Commands;
using TitanGlobal.Application.UseCases.Modulo23_Almacenes.Queries;

namespace TitanGlobal.Api.Controllers.Modulo23_Almacenes;

[ApiController]
[Route("api/v1/almacenes")]
public class AlmacenesController : ControllerBase
{
    private readonly ISender _sender;

    public AlmacenesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? ciudad,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAlmacenesDto resultado = await _sender.Send(new ListarAlmacenesQuery
        {
            Busqueda = busqueda,
            Ciudad = ciudad,
            Tipo = tipo,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("ciudades")]
    public async Task<IActionResult> Ciudades()
    {
        List<string> resultado = await _sender.Send(new ListarCiudadesAlmacenQuery());

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenAlmacenesDto resultado = await _sender.Send(new ResumenAlmacenesQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        AlmacenDetalleDto resultado = await _sender.Send(new ObtenerAlmacenQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarAlmacenRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearAlmacenCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarAlmacenRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarAlmacenCommand { Id = id, Solicitud = solicitud });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarAlmacenCommand { Id = id });

        return NoContent();
    }
}
