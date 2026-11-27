using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

namespace TitanGlobal.Api.Controllers.Modulo70_SeguridadSaludOcupacional;

[ApiController]
[Route("api/v1/equipos-proteccion")]
public class EquiposProteccionController : ControllerBase
{
    private readonly ISender _sender;

    public EquiposProteccionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaEquiposProteccionDto resultado = await _sender.Send(new ListarEquiposProteccionQuery
        {
            Busqueda = busqueda,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        EquipoProteccionListadoDto resultado = await _sender.Send(new ObtenerEquipoProteccionQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarEquipoProteccionRequestDto solicitud)
    {
        EquipoProteccionIdDto resultado = await _sender.Send(new CrearEquipoProteccionCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarEquipoProteccionRequestDto solicitud)
    {
        EquipoProteccionIdDto resultado = await _sender.Send(new ActualizarEquipoProteccionCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/desactivacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Desactivar(int id)
    {
        EquipoProteccionIdDto resultado = await _sender.Send(new DesactivarEquipoProteccionCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/activacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        EquipoProteccionIdDto resultado = await _sender.Send(new ActivarEquipoProteccionCommand { Id = id });
        return Ok(resultado);
    }
}
