using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores.Commands;
using TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores.Queries;

namespace TitanGlobal.Api.Controllers.Modulo12_EvaluacionProveedores;

[ApiController]
[Route("api/v1/evaluaciones-proveedores")]
public class EvaluacionesProveedorController : ControllerBase
{
    private readonly ISender _sender;

    public EvaluacionesProveedorController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? proveedorId,
        [FromQuery] string? periodo,
        [FromQuery] string? calificacion,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaEvaluacionesDto resultado = await _sender.Send(new ListarEvaluacionesProveedorQuery
        {
            ProveedorId = proveedorId,
            Periodo = periodo,
            Calificacion = calificacion,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenEvaluacionesDto resultado = await _sender.Send(new ResumenEvaluacionesProveedorQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        EvaluacionDetalleDto resultado = await _sender.Send(new ObtenerEvaluacionProveedorQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearEvaluacionRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearEvaluacionProveedorCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarEvaluacionRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarEvaluacionProveedorCommand
        {
            Id = id,
            Solicitud = solicitud
        });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarEvaluacionProveedorCommand { Id = id });

        return NoContent();
    }
}
