using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Dtos;
using TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.UseCases;

namespace TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Controllers;

[ApiController]
[Route("api/v1/ordenes-compra")]
public class OrdenesCompraController : ControllerBase
{
    private readonly CrearOrdenCompraUseCase _crearOrdenCompraUseCase;

    private readonly AnaliticaOrdenesCompraUseCase _analiticaOrdenesCompraUseCase;

    public OrdenesCompraController(
        CrearOrdenCompraUseCase crearOrdenCompraUseCase,
        AnaliticaOrdenesCompraUseCase analiticaOrdenesCompraUseCase)
    {
        _crearOrdenCompraUseCase = crearOrdenCompraUseCase;
        _analiticaOrdenesCompraUseCase = analiticaOrdenesCompraUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearOrdenCompraRequestDto request)
    {
        int id = await _crearOrdenCompraUseCase.ExecuteAsync(request);

        return Ok(new { Id = id });
    }

    [HttpGet("productos/filtro")]
    public async Task<IActionResult> BuscarProductosPorFiltro(
        [FromQuery] string termino,
        [FromQuery] decimal minPrecio)
    {
        List<ProductoResumenDto> products = await _analiticaOrdenesCompraUseCase
            .BuscarProductosPorFiltroAsync(termino, minPrecio);

        return Ok(products);
    }

    [HttpGet("ventas/suma")]
    public async Task<IActionResult> ObtenerSumaVentasDesdeFecha([FromQuery] DateTime fechaInicio)
    {
        decimal totalVentas = await _analiticaOrdenesCompraUseCase.ObtenerSumaVentasDesdeFechaAsync(fechaInicio);

        return Ok(new { TotalVentas = totalVentas });
    }

    [HttpGet("productos/mas-caro")]
    public async Task<IActionResult> ObtenerProductoMasCaro()
    {
        ProductoResumenDto? product = await _analiticaOrdenesCompraUseCase.ObtenerProductoMasCaroAsync();

        return Ok(product);
    }

    [HttpGet("clientes/{clienteId:int}/promedio")]
    public async Task<IActionResult> ObtenerPromedioTotalPorCliente(int clienteId)
    {
        decimal average = await _analiticaOrdenesCompraUseCase.ObtenerPromedioTotalPorClienteAsync(clienteId);

        return Ok(new { ClienteId = clienteId, PromedioCompra = average });
    }

    [HttpGet("productos/sin-descripcion")]
    public async Task<IActionResult> ObtenerProductosSinDescripcion()
    {
        List<ProductoResumenDto> products = await _analiticaOrdenesCompraUseCase.ObtenerProductosSinDescripcionAsync();

        return Ok(products);
    }

    [HttpGet("clientes/frecuencia")]
    public async Task<IActionResult> ObtenerClientesConMasOrdenes()
    {
        List<ClienteFrecuenciaDto> clients = await _analiticaOrdenesCompraUseCase.ObtenerClientesConMasOrdenesAsync();

        return Ok(clients);
    }

    [HttpGet("clientes/{clienteId:int}/jerarquia")]
    public async Task<IActionResult> ObtenerOrdenesJerarquicas(int clienteId)
    {
        List<OrdenCompraJerarquicaDto> orders = await _analiticaOrdenesCompraUseCase
            .ObtenerOrdenesJerarquicasAsync(clienteId);

        return Ok(orders);
    }

    [HttpGet("clientes/{clienteId:int}/categorias-unicas")]
    public async Task<IActionResult> ObtenerCategoriasUnicasCompradasPorCliente(int clienteId)
    {
        List<string> categories = await _analiticaOrdenesCompraUseCase
            .ObtenerCategoriasUnicasCompradasPorClienteAsync(clienteId);

        return Ok(categories);
    }

    [HttpGet("productos/{productoId:int}/clientes-compradores")]
    public async Task<IActionResult> ObtenerClientesQueCompraronProducto(int productoId)
    {
        List<string> clients = await _analiticaOrdenesCompraUseCase
            .ObtenerClientesQueCompraronProductoAsync(productoId);

        return Ok(clients);
    }
}
