using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Commands;
using TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

namespace TitanGlobal.Api.Controllers.Modulo04_OrdenesCompra;

[ApiController]
[Route("api/v1/ordenes-compra")]
public class OrdenesCompraController : ControllerBase
{
    private readonly ISender _sender;

    public OrdenesCompraController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearOrdenCompraRequestDto request)
    {
        int id = await _sender.Send(new CrearOrdenCompraCommand { Request = request });

        return Ok(new { Id = id });
    }

    [HttpGet("productos/filtro")]
    public async Task<IActionResult> BuscarProductosPorFiltro(
        [FromQuery] string termino,
        [FromQuery] decimal minPrecio)
    {
        List<ProductoResumenDto> products = await _sender.Send(new BuscarProductosPorFiltroQuery
        {
            Termino = termino,
            MinPrecio = minPrecio
        });

        return Ok(products);
    }

    [HttpGet("ventas/suma")]
    public async Task<IActionResult> ObtenerSumaVentasDesdeFecha([FromQuery] DateTime fechaInicio)
    {
        decimal totalVentas = await _sender.Send(new ObtenerSumaVentasDesdeFechaQuery
        {
            FechaInicio = fechaInicio
        });

        return Ok(new { TotalVentas = totalVentas });
    }

    [HttpGet("productos/mas-caro")]
    public async Task<IActionResult> ObtenerProductoMasCaro()
    {
        ProductoResumenDto? product = await _sender.Send(new ObtenerProductoMasCaroQuery());

        return Ok(product);
    }

    [HttpGet("clientes/{clienteId:int}/promedio")]
    public async Task<IActionResult> ObtenerPromedioTotalPorCliente(int clienteId)
    {
        decimal average = await _sender.Send(new ObtenerPromedioTotalPorClienteQuery { ClienteId = clienteId });

        return Ok(new { ClienteId = clienteId, PromedioCompra = average });
    }

    [HttpGet("productos/sin-descripcion")]
    public async Task<IActionResult> ObtenerProductosSinDescripcion()
    {
        List<ProductoResumenDto> products = await _sender.Send(new ObtenerProductosSinDescripcionQuery());

        return Ok(products);
    }

    [HttpGet("clientes/frecuencia")]
    public async Task<IActionResult> ObtenerClientesConMasOrdenes()
    {
        List<ClienteFrecuenciaDto> clients = await _sender.Send(new ObtenerClientesConMasOrdenesQuery());

        return Ok(clients);
    }

    [HttpGet("clientes/{clienteId:int}/jerarquia")]
    public async Task<IActionResult> ObtenerOrdenesJerarquicas(int clienteId)
    {
        List<OrdenCompraJerarquicaDto> orders = await _sender.Send(new ObtenerOrdenesJerarquicasQuery
        {
            ClienteId = clienteId
        });

        return Ok(orders);
    }

    [HttpGet("clientes/{clienteId:int}/categorias-unicas")]
    public async Task<IActionResult> ObtenerCategoriasUnicasCompradasPorCliente(int clienteId)
    {
        List<string> categories = await _sender.Send(new ObtenerCategoriasUnicasCompradasPorClienteQuery
        {
            ClienteId = clienteId
        });

        return Ok(categories);
    }

    [HttpGet("productos/{productoId:int}/clientes-compradores")]
    public async Task<IActionResult> ObtenerClientesQueCompraronProducto(int productoId)
    {
        List<string> clients = await _sender.Send(new ObtenerClientesQueCompraronProductoQuery
        {
            ProductoId = productoId
        });

        return Ok(clients);
    }
}
