using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Commands;
using TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Queries;

namespace TitanGlobal.Api.Controllers.Modulo48_TransportistasTerceros;

[ApiController]
[Route("api/v1/fletes-terceros")]
public class FletesTercerosController : ControllerBase
{
    private readonly ISender _sender;

    public FletesTercerosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? transportistaId, [FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaFletesTercerosDto resultado = await _sender.Send(new ListarFletesTercerosQuery
        {
            TransportistaId = transportistaId,
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
        FleteTerceroDetalleDto resultado = await _sender.Send(new ObtenerFleteTerceroQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarFleteTerceroRequestDto solicitud)
    {
        int resultado = await _sender.Send(new RegistrarFleteTerceroCommand { Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/cancelacion")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarFleteTerceroRequestDto solicitud)
    {
        int resultado = await _sender.Send(new CancelarFleteTerceroCommand { Id = id, Solicitud = solicitud });
        return Ok(new { Id = resultado });
    }
}
