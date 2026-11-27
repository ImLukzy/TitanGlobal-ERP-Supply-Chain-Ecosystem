using MediatR;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas.Commands;
using TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas.Queries;

namespace TitanGlobal.Api.Controllers.Modulo75_ImpuestosTasas;

[ApiController]
[Route("api/v1/impuestos")]
public class ImpuestosController : ControllerBase
{
    private readonly ISender _sender;

    public ImpuestosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _sender.Send(new ListarImpuestosQuery()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        return Ok(await _sender.Send(new ObtenerImpuestoQuery { Id = id }));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarImpuestoRequestDto dto)
    {
        int id = await _sender.Send(new CrearImpuestoCommand { Dto = dto });
        return Created($"/api/v1/impuestos/{id}", new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] GuardarImpuestoRequestDto dto)
    {
        await _sender.Send(new ActualizarImpuestoCommand { Id = id, Dto = dto });
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _sender.Send(new DesactivarImpuestoCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id:int}/tasas")]
    public async Task<IActionResult> RegistrarTasa(
        int id,
        [FromBody] GuardarTasaImpuestoRequestDto dto)
    {
        int tasaId = await _sender.Send(new RegistrarTasaImpuestoCommand { ImpuestoId = id, Dto = dto });
        return StatusCode(201, new { id = tasaId, impuestoId = id });
    }

    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular(
        [FromBody] CalcularImpuestoRequestDto dto)
    {
        return Ok(await _sender.Send(new CalcularImpuestoQuery { Dto = dto }));
    }
}
