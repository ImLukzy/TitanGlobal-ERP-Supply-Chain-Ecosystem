using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Application.UseCases.Modulo01_Clientes.Commands;
using TitanGlobal.Application.UseCases.Modulo01_Clientes.Queries;

namespace TitanGlobal.Api.Controllers.Modulo01_Clientes;

[ApiController]
[Route("api/v1/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ISender _sender;

    public ClientesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaClientesDto resultado = await _sender.Send(new ListarClientesQuery
        {
            Busqueda = busqueda,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenClientesDto resultado = await _sender.Send(new ResumenClientesQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ClienteDetalleDto resultado = await _sender.Send(new ObtenerClienteQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarClienteRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearClienteCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarClienteRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarClienteCommand { Id = id, Solicitud = solicitud });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarClienteCommand { Id = id });

        return NoContent();
    }
}
