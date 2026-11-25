using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

namespace TitanGlobal.Api.Controllers.Modulo69_Capacitaciones;

[ApiController]
[Route("api/v1/capacitaciones")]
public class CapacitacionesController : ControllerBase
{
    private readonly ConsultarCapacitacionesUseCase _consultarCapacitaciones;

    public CapacitacionesController(ConsultarCapacitacionesUseCase consultarCapacitaciones)
    {
        _consultarCapacitaciones = consultarCapacitaciones;
    }

    [HttpGet("horas")]
    public async Task<IActionResult> ListarHoras(
        [FromQuery] int? anio,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaHorasDto resultado = await _consultarCapacitaciones.ListarHorasAsync(anio, busqueda, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("empleados/{empleadoId:int}")]
    public async Task<IActionResult> ObtenerEmpleado(int empleadoId, [FromQuery] int? anio)
    {
        CapacitacionEmpleadoDto resultado = await _consultarCapacitaciones.ObtenerEmpleadoAsync(empleadoId, anio);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] int? anio)
    {
        ResumenCapacitacionesDto resultado = await _consultarCapacitaciones.ResumenAsync(anio);
        return Ok(resultado);
    }
}
