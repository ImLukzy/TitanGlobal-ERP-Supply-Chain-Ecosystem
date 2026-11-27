using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

namespace TitanGlobal.Api.Controllers.Modulo69_Capacitaciones;

[ApiController]
[Route("api/v1/capacitaciones")]
public class CapacitacionesController : ControllerBase
{
    private readonly ISender _sender;

    public CapacitacionesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("horas")]
    public async Task<IActionResult> ListarHoras(
        [FromQuery] int? anio,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaHorasDto resultado = await _sender.Send(new ListarHorasQuery
        {
            Anio = anio,
            Busqueda = busqueda,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("empleados/{empleadoId:int}")]
    public async Task<IActionResult> ObtenerEmpleado(int empleadoId, [FromQuery] int? anio)
    {
        CapacitacionEmpleadoDto resultado = await _sender.Send(new ObtenerEmpleadoQuery
        {
            EmpleadoId = empleadoId,
            Anio = anio
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] int? anio)
    {
        ResumenCapacitacionesDto resultado = await _sender.Send(new ResumenCapacitacionesQuery { Anio = anio });
        return Ok(resultado);
    }
}
