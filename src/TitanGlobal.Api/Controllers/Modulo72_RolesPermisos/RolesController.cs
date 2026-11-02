using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Application.UseCases.Modulo72_RolesPermisos;

namespace TitanGlobal.Api.Controllers.Modulo72_RolesPermisos;

[ApiController]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly AdministrarRolesUseCase _administrarRoles;
    private readonly EliminarRolUseCase _eliminarRol;

    public RolesController(AdministrarRolesUseCase administrarRoles, EliminarRolUseCase eliminarRol)
    {
        _administrarRoles = administrarRoles;
        _eliminarRol = eliminarRol;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaRolesDto resultado = await _administrarRoles.ListarAsync(busqueda, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("matriz")]
    public async Task<IActionResult> Matriz()
    {
        MatrizPermisosDto resultado = await _administrarRoles.MatrizAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        RolDetalleDto resultado = await _administrarRoles.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarRolRequestDto solicitud)
    {
        int id = await _administrarRoles.CrearAsync(solicitud);
        return Ok(new RolIdDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarRolRequestDto solicitud)
    {
        int actualizado = await _administrarRoles.ActualizarAsync(id, solicitud);
        return Ok(new RolIdDto { Id = actualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarRol.EjecutarAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/permisos")]
    public async Task<IActionResult> ObtenerPermisos(int id)
    {
        List<PermisoModuloDto> permisos = await _administrarRoles.ObtenerPermisosAsync(id);
        return Ok(permisos);
    }

    [HttpPut("{id:int}/permisos")]
    public async Task<IActionResult> GuardarPermisos(int id, [FromBody] GuardarPermisosRequestDto solicitud)
    {
        List<PermisoModuloDto> permisos = await _administrarRoles.GuardarPermisosAsync(id, solicitud);
        return Ok(permisos);
    }
}
