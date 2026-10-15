using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo71_Usuarios.Dtos;
using TitanGlobal.Api.Modules.Modulo71_Usuarios.UseCases;

namespace TitanGlobal.Api.Modules.Modulo71_Usuarios.Controllers;

[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly AdministrarUsuariosUseCase _administrarUsuarios;

    public UsuariosController(AdministrarUsuariosUseCase administrarUsuarios)
    {
        _administrarUsuarios = administrarUsuarios;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? rolId,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaUsuariosDto resultado = await _administrarUsuarios.ListarAsync(
            busqueda,
            rolId,
            activo,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenUsuariosDto resultado = await _administrarUsuarios.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        UsuarioDetalleDto resultado = await _administrarUsuarios.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarUsuarioRequestDto solicitud)
    {
        int id = await _administrarUsuarios.CrearAsync(solicitud);
        return Ok(new UsuarioIdDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarUsuarioRequestDto solicitud)
    {
        int actualizado = await _administrarUsuarios.ActualizarAsync(id, solicitud);
        return Ok(new UsuarioIdDto { Id = actualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _administrarUsuarios.EliminarAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/permisos")]
    public async Task<IActionResult> Permisos(int id)
    {
        List<PermisoEfectivoDto> resultado = await _administrarUsuarios.PermisosAsync(id);
        return Ok(resultado);
    }
}
