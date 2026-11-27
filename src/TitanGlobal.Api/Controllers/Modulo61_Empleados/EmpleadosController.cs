using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Application.UseCases.Modulo61_Empleados.Commands;
using TitanGlobal.Application.UseCases.Modulo61_Empleados.Queries;

namespace TitanGlobal.Api.Controllers.Modulo61_Empleados;

[ApiController]
[Route("api/v1/empleados")]
public class EmpleadosController : ControllerBase
{
    private readonly ISender _sender;

    public EmpleadosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaEmpleadosDto resultado = await _sender.Send(new ListarEmpleadosQuery
        {
            Busqueda = busqueda,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        EmpleadoDetalleDto resultado = await _sender.Send(new ObtenerEmpleadoQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarEmpleadoRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearEmpleadoCommand { Solicitud = solicitud });
        return Ok(new EmpleadoIdDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarEmpleadoRequestDto solicitud)
    {
        int actualizado = await _sender.Send(new ActualizarEmpleadoCommand { Id = id, Solicitud = solicitud });
        return Ok(new EmpleadoIdDto { Id = actualizado });
    }

    [HttpPost("{id:int}/cese")]
    public async Task<IActionResult> Cesar(int id, [FromBody] CeseEmpleadoRequestDto solicitud)
    {
        int cesado = await _sender.Send(new CesarEmpleadoCommand { Id = id, Solicitud = solicitud });
        return Ok(new EmpleadoIdDto { Id = cesado });
    }
}
