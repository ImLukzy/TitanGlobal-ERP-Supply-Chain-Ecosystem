using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Commands;
using TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Queries;

namespace TitanGlobal.Api.Controllers.Modulo48_TransportistasTerceros;

[ApiController]
[Route("api/v1/transportistas-terceros")]
public class TransportistasTercerosController : ControllerBase
{
    private readonly ISender _sender;

    public TransportistasTercerosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? pais, [FromQuery] string? tipoServicio, [FromQuery] bool? activo, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaTransportistasTercerosDto resultado = await _sender.Send(new ListarTransportistasTercerosQuery
        {
            Busqueda = busqueda,
            Pais = pais,
            TipoServicio = tipoServicio,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenTransportistasTercerosDto resultado = await _sender.Send(new ResumenTransportistasTercerosQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        TransportistaTerceroDetalleDto resultado = await _sender.Send(new ObtenerTransportistaTerceroQuery
        {
            Id = id
        });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarTransportistaTerceroRequestDto solicitud)
    {
        int resultado = await _sender.Send(new CrearTransportistaTerceroCommand { Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarTransportistaTerceroRequestDto solicitud)
    {
        int resultado = await _sender.Send(new ActualizarTransportistaTerceroCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(new { Id = resultado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarTransportistaTerceroCommand { Id = id });
        return NoContent();
    }
}
