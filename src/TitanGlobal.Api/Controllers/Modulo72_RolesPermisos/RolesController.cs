using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Commands;
using TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Queries;

namespace TitanGlobal.Api.Controllers.Modulo72_RolesPermisos;

[ApiController]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaRolesDto resultado = await _sender.Send(new ListarRolesQuery
        {
            Busqueda = busqueda,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("matriz")]
    public async Task<IActionResult> Matriz()
    {
        MatrizPermisosDto resultado = await _sender.Send(new ObtenerMatrizPermisosQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        RolDetalleDto resultado = await _sender.Send(new ObtenerRolQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarRolRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearRolCommand { Solicitud = solicitud });
        return Ok(new RolIdDto { Id = id });
    }

    [HttpPut("{id:int}")]

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarRolRequestDto solicitud)
    {
        int actualizado = await _sender.Send(new ActualizarRolCommand { Id = id, Solicitud = solicitud });
        return Ok(new RolIdDto { Id = actualizado });
    }

    [HttpDelete("{id:int}")]

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarRolCommand { Id = id });
        return NoContent();
    }

    [HttpGet("{id:int}/permisos")]
    public async Task<IActionResult> ObtenerPermisos(int id)
    {
        List<PermisoModuloDto> permisos = await _sender.Send(new ObtenerPermisosQuery { Id = id });
        return Ok(permisos);
    }

    [HttpPut("{id:int}/permisos")]

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GuardarPermisos(int id, [FromBody] GuardarPermisosRequestDto solicitud)
    {
        List<PermisoModuloDto> permisos = await _sender.Send(new GuardarPermisosCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(permisos);
    }
}
