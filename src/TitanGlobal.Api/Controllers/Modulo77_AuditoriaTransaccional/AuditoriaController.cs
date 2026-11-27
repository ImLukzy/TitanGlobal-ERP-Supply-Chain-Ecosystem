using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;
using TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional.Queries;

namespace TitanGlobal.Api.Controllers.Modulo77_AuditoriaTransaccional;

[ApiController]
[Route("api/v1/auditoria")]
public class AuditoriaController : ControllerBase
{
    private readonly ISender _sender;

    public AuditoriaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? entidad,
        [FromQuery] string? entidadId,
        [FromQuery] string? accion,
        [FromQuery] string? usuario,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAuditoriaDto resultado = await _sender.Send(new ListarAuditoriaQuery
        {
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Usuario = usuario,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("entidades")]
    public async Task<IActionResult> ListarEntidades()
    {
        List<string> resultado = await _sender.Send(new ListarEntidadesAuditoriaQuery());
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenAuditoriaDto resultado = await _sender.Send(new ResumenAuditoriaQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id)
    {
        RegistroDetalleDto resultado = await _sender.Send(new ObtenerRegistroAuditoriaQuery { Id = id });
        return Ok(resultado);
    }
}
