using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;
using TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

namespace TitanGlobal.Api.Controllers.Modulo66_Planillas;

[ApiController]
[Route("api/v1/planillas")]
public class PlanillasController : ControllerBase
{
    private readonly ISender _sender;

    public PlanillasController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? anio,
        [FromQuery] string? tipo,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaPlanillasDto resultado = await _sender.Send(new ListarPlanillasQuery
        {
            Anio = anio,
            Tipo = tipo,
            Estado = estado,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenPlanillasDto resultado = await _sender.Send(new ResumenPlanillasQuery());
        return Ok(resultado);
    }

    [HttpGet("parametros")]
    public async Task<IActionResult> ObtenerParametros()
    {
        ParametrosPlanillaDto resultado = await _sender.Send(new ObtenerParametrosQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        PlanillaDetalleDto resultado = await _sender.Send(new ObtenerPlanillaQuery { Id = id });
        return Ok(resultado);
    }

    [HttpGet("{id:int}/detalles")]
    public async Task<IActionResult> ListarDetalles(
        int id,
        [FromQuery] string? busqueda,
        [FromQuery] bool soloIncidencias = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaPlanillaEmpleadosDto resultado = await _sender.Send(new ListarDetallesQuery
        {
            Id = id,
            Busqueda = busqueda,
            SoloIncidencias = soloIncidencias,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("boletas/{detalleId:int}")]
    public async Task<IActionResult> ObtenerBoleta(int detalleId)
    {
        BoletaPagoDto resultado = await _sender.Send(new ObtenerBoletaQuery { DetalleId = detalleId });
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] CrearPlanillaRequestDto solicitud)
    {
        PlanillaCreadaDto resultado = await _sender.Send(new CrearPlanillaCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/calculo")]
    [PermitirCuerpoVacio]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Calcular(int id)
    {
        PlanillaCalculoDto resultado = await _sender.Send(new CalcularPlanillaCommand
        {
            Id = id,
            Usuario = ObtenerUsuario()
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/aprobacion")]
    [PermitirCuerpoVacio]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Aprobar(int id)
    {
        PlanillaIdDto resultado = await _sender.Send(new AprobarPlanillaCommand
        {
            Id = id,
            Usuario = ObtenerUsuario()
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/pago")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Pagar(int id, [FromBody] PagarPlanillaRequestDto solicitud)
    {
        PlanillaIdDto resultado = await _sender.Send(new PagarPlanillaCommand { Id = id, Solicitud = solicitud });
        return Ok(resultado);
    }

    // El JWT nombra la reclamación "name" y AutenticacionJwtExtensions la usa como nombre de la identidad.
    private string ObtenerUsuario()
    {
        return User.Identity?.Name ?? string.Empty;
    }
}
