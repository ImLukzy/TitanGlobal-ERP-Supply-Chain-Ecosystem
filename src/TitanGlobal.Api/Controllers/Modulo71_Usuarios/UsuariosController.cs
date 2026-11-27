using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Application.UseCases.Modulo71_Usuarios.Commands;
using TitanGlobal.Application.UseCases.Modulo71_Usuarios.Queries;

namespace TitanGlobal.Api.Controllers.Modulo71_Usuarios;

[ApiController]
[Route("api/v1/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly ISender _sender;

    public UsuariosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? rolId,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaUsuariosDto resultado = await _sender.Send(new ListarUsuariosQuery
        {
            Busqueda = busqueda,
            RolId = rolId,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenUsuariosDto resultado = await _sender.Send(new ResumenUsuariosQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        UsuarioDetalleDto resultado = await _sender.Send(new ObtenerUsuarioQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarUsuarioRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearUsuarioCommand { Solicitud = solicitud });
        return Ok(new UsuarioIdDto { Id = id });
    }

    [HttpPut("{id:int}")]

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarUsuarioRequestDto solicitud)
    {
        int actualizado = await _sender.Send(new ActualizarUsuarioCommand { Id = id, Solicitud = solicitud });
        return Ok(new UsuarioIdDto { Id = actualizado });
    }

    [HttpDelete("{id:int}")]

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarUsuarioCommand { Id = id });
        return NoContent();
    }

    [HttpGet("{id:int}/permisos")]
    public async Task<IActionResult> Permisos(int id)
    {
        List<PermisoEfectivoDto> resultado = await _sender.Send(new ObtenerPermisosUsuarioQuery { Id = id });
        return Ok(resultado);
    }
}
