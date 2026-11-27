using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;
using TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

namespace TitanGlobal.Api.Controllers.Modulo69_Capacitaciones;

[ApiController]
[Route("api/v1/cursos")]
public class CursosController : ControllerBase
{
    private readonly ISender _sender;

    public CursosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaCursosDto resultado = await _sender.Send(new ListarCursosQuery
        {
            Busqueda = busqueda,
            Tipo = tipo,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        CursoListadoDto resultado = await _sender.Send(new ObtenerCursoQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarCursoRequestDto solicitud)
    {
        CursoIdDto resultado = await _sender.Send(new CrearCursoCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCursoRequestDto solicitud)
    {
        CursoIdDto resultado = await _sender.Send(new ActualizarCursoCommand { Id = id, Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/desactivacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Desactivar(int id)
    {
        CursoIdDto resultado = await _sender.Send(new DesactivarCursoCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/activacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        CursoIdDto resultado = await _sender.Send(new ActivarCursoCommand { Id = id });
        return Ok(resultado);
    }
}
