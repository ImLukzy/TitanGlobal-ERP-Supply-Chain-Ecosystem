using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;
using TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Queries;

namespace TitanGlobal.Api.Controllers.Modulo68_EvaluacionDesempeno;

[ApiController]
[Route("api/v1/evaluaciones")]
public class EvaluacionesController : ControllerBase
{
    private readonly ISender _sender;

    public EvaluacionesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? cicloId,
        [FromQuery] int? empleadoId,
        [FromQuery] string? estado,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaEvaluacionesDto resultado = await _sender.Send(new ListarEvaluacionesQuery
        {
            CicloId = cicloId,
            EmpleadoId = empleadoId,
            Estado = estado,
            Busqueda = busqueda,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        EvaluacionDetalleDto resultado = await _sender.Send(new ObtenerEvaluacionQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] CrearEvaluacionRequestDto solicitud)
    {
        EvaluacionIdDto resultado = await _sender.Send(new CrearEvaluacionCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPut("{id:int}/calificaciones")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> GuardarCalificaciones(int id, [FromBody] GuardarCalificacionesRequestDto solicitud)
    {
        EvaluacionIdDto resultado = await _sender.Send(new GuardarCalificacionesCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/envio")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Enviar(int id)
    {
        EvaluacionIdDto resultado = await _sender.Send(new EnviarEvaluacionCommand { Id = id });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cierre")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cerrar(int id)
    {
        EvaluacionIdDto resultado = await _sender.Send(new CerrarEvaluacionCommand { Id = id });
        return Ok(resultado);
    }
}
