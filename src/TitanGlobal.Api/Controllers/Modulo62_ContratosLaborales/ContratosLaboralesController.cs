using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Commands;
using TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Queries;

namespace TitanGlobal.Api.Controllers.Modulo62_ContratosLaborales;

[ApiController]
[Route("api/v1/contratos-laborales")]
public class ContratosLaboralesController : ControllerBase
{
    private readonly ISender _sender;

    public ContratosLaboralesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? modalidad,
        [FromQuery] int? empleadoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaContratosLaboralesDto resultado = await _sender.Send(new ListarContratosLaboralesQuery
        {
            Busqueda = busqueda,
            Estado = estado,
            Modalidad = modalidad,
            EmpleadoId = empleadoId,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ContratoLaboralDetalleDto resultado = await _sender.Send(new ObtenerContratoLaboralQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarContratoLaboralRequestDto solicitud)
    {
        ContratoLaboralCreadoDto resultado = await _sender.Send(new CrearContratoLaboralCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/renovacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Renovar(int id, [FromBody] RenovarContratoLaboralRequestDto solicitud)
    {
        ContratoLaboralCreadoDto resultado = await _sender.Send(new RenovarContratoLaboralCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/adendas")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> AgregarAdenda(int id, [FromBody] AdendaContratoRequestDto solicitud)
    {
        ContratoLaboralIdDto resultado = await _sender.Send(new AgregarAdendaCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/anulacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularContratoLaboralRequestDto solicitud)
    {
        ContratoLaboralIdDto resultado = await _sender.Send(new AnularContratoLaboralCommand
        {
            Id = id,
            Solicitud = solicitud
        });
        return Ok(resultado);
    }
}
