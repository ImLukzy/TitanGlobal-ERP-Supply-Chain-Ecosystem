using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Commands;
using TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Queries;

namespace TitanGlobal.Api.Controllers.Modulo67_AdelantosPrestamos;

[ApiController]
[Route("api/v1/prestamos")]
public class PrestamosController : ControllerBase
{
    private readonly ISender _sender;

    public PrestamosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? empleadoId,
        [FromQuery] string? tipo,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaPrestamosDto resultado = await _sender.Send(new ListarPrestamosQuery
        {
            Busqueda = busqueda,
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenPrestamosDto resultado = await _sender.Send(new ResumenPrestamosQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        PrestamoDetalleDto resultado = await _sender.Send(new ObtenerPrestamoQuery { Id = id });
        return Ok(resultado);
    }

    [HttpGet("capacidad/{empleadoId:int}")]
    public async Task<IActionResult> ObtenerCapacidad(int empleadoId, [FromQuery] DateOnly? fecha)
    {
        CapacidadPrestamoDto resultado = await _sender.Send(new ObtenerCapacidadQuery
        {
            EmpleadoId = empleadoId,
            Fecha = fecha
        });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPrestamoRequestDto solicitud)
    {
        PrestamoCreadoDto resultado = await _sender.Send(new RegistrarPrestamoCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/anulacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularPrestamoRequestDto solicitud)
    {
        PrestamoIdDto resultado = await _sender.Send(new AnularPrestamoCommand { Id = id, Solicitud = solicitud });
        return Ok(resultado);
    }
}
