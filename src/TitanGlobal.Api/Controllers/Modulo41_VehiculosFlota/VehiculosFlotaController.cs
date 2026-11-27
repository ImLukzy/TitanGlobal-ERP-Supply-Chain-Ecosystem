using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Commands;
using TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Queries;

namespace TitanGlobal.Api.Controllers.Modulo41_VehiculosFlota;

[ApiController]
[Route("api/v1/vehiculos-flota")]
public class VehiculosFlotaController : ControllerBase
{
    private readonly ISender _sender;

    public VehiculosFlotaController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaVehiculosFlotaDto resultado = await _sender.Send(new ListarVehiculosFlotaQuery
        {
            Busqueda = busqueda,
            Tipo = tipo,
            Activo = activo,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });

        return Ok(resultado);
    }

    [HttpGet("tipos")]
    public async Task<IActionResult> Tipos()
    {
        List<string> resultado = await _sender.Send(new ListarTiposVehiculoFlotaQuery());

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenVehiculosFlotaDto resultado = await _sender.Send(new ResumenVehiculosFlotaQuery());

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        VehiculoFlotaDetalleDto resultado = await _sender.Send(new ObtenerVehiculoFlotaQuery { Id = id });

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarVehiculoFlotaRequestDto solicitud)
    {
        int id = await _sender.Send(new CrearVehiculoFlotaCommand { Solicitud = solicitud });

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarVehiculoFlotaRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new ActualizarVehiculoFlotaCommand
        {
            Id = id,
            Solicitud = solicitud
        });

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarVehiculoFlotaCommand { Id = id });

        return NoContent();
    }
}
