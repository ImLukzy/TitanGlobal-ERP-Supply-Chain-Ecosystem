using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo08_ComisionesVendedores;

[Collection("Modulo04")]
public class ComisionesVendedoresApiTests
{
    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _clienteHttp;
    private int _anaId;
    private int _luisId;
    private int _inactivoId;

    public ComisionesVendedoresApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Theory]
    [InlineData("", "Ana", null, 5, "El código es obligatorio.")]
    [InlineData("a!", "Ana", null, 5, "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("AA", " ", null, 5, "El nombre del vendedor es obligatorio.")]
    [InlineData("AA", "Ana", "invalido", 5, "El email no tiene un formato válido.")]
    [InlineData("AA", "Ana", null, -0.01, "La comisión debe estar entre 0 y 20 %.")]
    [InlineData("AA", "Ana", null, 20.01, "La comisión debe estar entre 0 y 20 %.")]
    public async Task ValidacionDelVendedorDevuelveMensajeLiteralSinCambiarFilas(
        string codigo,
        string nombre,
        string? email,
        decimal porcentaje,
        string mensaje)
    {
        int filasAntes = await ContarVendedoresAsync();
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/vendedores",
            new GuardarVendedorRequestDto
            {
                Codigo = codigo,
                Nombre = nombre,
                Email = email,
                PorcentajeComision = porcentaje,
                Activo = true
            });

        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
        Assert.Equal(filasAntes, await ContarVendedoresAsync());
    }

    [Fact]
    public async Task NombreMayorACienDevuelveMensajeLiteralSinCrearFila()
    {
        int filasAntes = await ContarVendedoresAsync();
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/vendedores",
            Solicitud("VENDEDOR-LARGO", new string('N', 101), 5m));
        await AfirmarErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            "El nombre del vendedor no puede superar 100 caracteres.");
        Assert.Equal(filasAntes, await ContarVendedoresAsync());
    }

    [Fact]
    public async Task EmailMayorA150DevuelveMensajeLiteralSinCrearFila()
    {
        int filasAntes = await ContarVendedoresAsync();
        string email = $"{new string('a', 145)}@b.com";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/vendedores",
            new GuardarVendedorRequestDto
            {
                Codigo = "VENDEDOR-EMAIL",
                Nombre = "Ana",
                Email = email,
                PorcentajeComision = 5m,
                Activo = true
            });
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, "El email no tiene un formato válido.");
        Assert.Equal(filasAntes, await ContarVendedoresAsync());
    }

    [Fact]
    public async Task CodigoDuplicadoDevuelveConflictoLiteralSinCambiarFilas()
    {
        int filasAntes = await ContarVendedoresAsync();
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/vendedores", Solicitud("v-ana", "Otra Ana", 6m));
        await AfirmarErrorAsync(respuesta, HttpStatusCode.Conflict, "Ya existe un vendedor con el código V-ANA.");
        Assert.Equal(filasAntes, await ContarVendedoresAsync());
    }

    [Theory]
    [InlineData("GET", "/api/v1/comisiones-vendedores/vendedores/9999", "", 404, "El vendedor 9999 no existe.")]
    [InlineData("DELETE", "/api/v1/comisiones-vendedores/vendedores/9999", "", 404, "El vendedor 9999 no existe.")]
    [InlineData("PUT", "/api/v1/comisiones-vendedores/ordenes/9999/vendedor", "{\"vendedorId\":1}", 404, "La orden de compra 9999 no existe.")]
    [InlineData("PUT", "/api/v1/comisiones-vendedores/ordenes/1/vendedor", "{\"vendedorId\":9999}", 400, "El vendedor 9999 no existe.")]
    [InlineData("DELETE", "/api/v1/comisiones-vendedores/ordenes/8/vendedor", "", 404, "La orden OC-008 no tiene vendedor.")]
    public async Task ErroresDeEntidadesYAsignacionNoCambianFilas(
        string verbo,
        string ruta,
        string json,
        int codigo,
        string mensaje)
    {
        int asignacionesAntes = await ContarAsignacionesAsync();
        using HttpRequestMessage solicitud = CrearSolicitudHttp(verbo, ruta, json);
        HttpResponseMessage respuesta = await _clienteHttp.SendAsync(solicitud);
        await AfirmarErrorAsync(respuesta, (HttpStatusCode)codigo, mensaje);
        Assert.Equal(asignacionesAntes, await ContarAsignacionesAsync());
    }

    [Fact]
    public async Task VendedorInactivoNoSePuedeAsignar()
    {
        int asignacionesAntes = await ContarAsignacionesAsync();
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            "/api/v1/comisiones-vendedores/ordenes/1/vendedor",
            new AsignarVendedorRequestDto { VendedorId = _inactivoId });
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, "El vendedor V-OLD está inactivo.");
        Assert.Equal(asignacionesAntes, await ContarAsignacionesAsync());
    }

    [Fact]
    public async Task EliminarVendedorConVentasDevuelveConflictoLiteral()
    {
        await AgregarAsignacionAsync(1, _anaId);
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync(
            $"/api/v1/comisiones-vendedores/vendedores/{_anaId}");
        await AfirmarErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            "No se puede eliminar el vendedor V-ANA porque tiene ventas asignadas.");
    }

    [Fact]
    public async Task MesBloqueadoNoPermiteAsignarYNoCambiaFilas()
    {
        await AgregarLiquidacionAsync(_anaId, 2026, 1, "Calculada");
        await AgregarAsignacionAsync(1, _anaId);
        int asignacionesAntes = await ContarAsignacionesAsync();
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            "/api/v1/comisiones-vendedores/ordenes/1/vendedor",
            new AsignarVendedorRequestDto { VendedorId = _anaId });
        await AfirmarErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            "El mes 01/2026 ya está liquidado; no se pueden cambiar sus asignaciones.");
        Assert.Equal(asignacionesAntes, await ContarAsignacionesAsync());

        HttpResponseMessage quitar = await _clienteHttp.DeleteAsync(
            "/api/v1/comisiones-vendedores/ordenes/1/vendedor");
        await AfirmarErrorAsync(
            quitar,
            HttpStatusCode.Conflict,
            "El mes 01/2026 ya está liquidado; no se pueden cambiar sus asignaciones.");
        Assert.Equal(asignacionesAntes, await ContarAsignacionesAsync());
    }

    [Theory]
    [InlineData("/api/v1/comisiones-vendedores/comisiones?anio=1999&mes=1", 400, "El año no es válido.")]
    [InlineData("/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=13", 400, "El mes no es válido.")]
    [InlineData("/api/v1/comisiones-vendedores/ordenes?mes=13", 400, "El mes no es válido.")]
    public async Task PeriodoInvalidoDevuelveMensajeLiteral(string ruta, int codigo, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);
        await AfirmarErrorAsync(respuesta, (HttpStatusCode)codigo, mensaje);
    }

    [Fact]
    public async Task LiquidarMesSinVentasAsignadasDevuelveErrorLiteralSinFilas()
    {
        int liquidacionesAntes = await ContarLiquidacionesAsync();
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/liquidaciones",
            new LiquidarMesRequestDto { Anio = 2026, Mes = 5 });
        await AfirmarErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            "No hay ventas asignadas en 05/2026.");
        Assert.Equal(liquidacionesAntes, await ContarLiquidacionesAsync());
    }

    [Fact]
    public async Task LiquidacionDuplicadaYPagoDuplicadoDevuelvenConflictosLiterales()
    {
        await AgregarAsignacionAsync(4, _anaId);
        HttpResponseMessage liquidacion = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/liquidaciones",
            new LiquidarMesRequestDto { Anio = 2026, Mes = 3 });
        Assert.Equal(HttpStatusCode.OK, liquidacion.StatusCode);
        ComisionesMesDto? comisiones = await liquidacion.Content.ReadFromJsonAsync<ComisionesMesDto>();
        Assert.NotNull(comisiones);
        int liquidacionId = Assert.Single(comisiones.Filas).LiquidacionId!.Value;
        int filasAntes = await ContarLiquidacionesAsync();
        HttpResponseMessage repetir = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/liquidaciones",
            new LiquidarMesRequestDto { Anio = 2026, Mes = 3 });
        await AfirmarErrorAsync(repetir, HttpStatusCode.Conflict, "El mes 03/2026 ya está liquidado.");
        Assert.Equal(filasAntes, await ContarLiquidacionesAsync());

        HttpResponseMessage pago = await _clienteHttp.PostAsync(
            $"/api/v1/comisiones-vendedores/liquidaciones/{liquidacionId}/pagar", null);
        Assert.Equal(HttpStatusCode.OK, pago.StatusCode);
        HttpResponseMessage repetirPago = await _clienteHttp.PostAsync(
            $"/api/v1/comisiones-vendedores/liquidaciones/{liquidacionId}/pagar", null);
        await AfirmarErrorAsync(
            repetirPago,
            HttpStatusCode.Conflict,
            $"La liquidación {liquidacionId} ya está pagada.");
    }

    [Fact]
    public async Task PagarLiquidacionInexistenteDevuelveMensajeLiteral()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(
            "/api/v1/comisiones-vendedores/liquidaciones/9999/pagar", null);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.NotFound, "La liquidación 9999 no existe.");
    }

    [Fact]
    public async Task CalculoIncluyeLimiteUtcRedondeaYConservaLiquidacionTrasCambiarPorcentaje()
    {
        await AgregarOrdenAsync(901, new DateTime(2026, 3, 31, 23, 59, 0, DateTimeKind.Utc), 100.10m, 1);
        await AgregarOrdenAsync(902, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 999m, 1);
        await AgregarAsignacionAsync(901, _anaId);
        await AgregarAsignacionAsync(902, _anaId);

        ComisionesMesDto? pendiente = await _clienteHttp.GetFromJsonAsync<ComisionesMesDto>(
            "/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=3");
        Assert.NotNull(pendiente);
        ComisionVendedorDto fila = Assert.Single(pendiente.Filas);
        Assert.Equal(100.10m, fila.Ventas);
        Assert.Equal(5.01m, fila.Comision);
        Assert.Equal(1, fila.Ordenes);
        Assert.Equal("Pendiente", fila.Estado);

        ComisionesMesDto? abril = await _clienteHttp.GetFromJsonAsync<ComisionesMesDto>(
            "/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=4");
        Assert.NotNull(abril);
        Assert.Equal(999m, Assert.Single(abril.Filas).Ventas);

        HttpResponseMessage liquidar = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/liquidaciones",
            new LiquidarMesRequestDto { Anio = 2026, Mes = 3 });
        Assert.Equal(HttpStatusCode.OK, liquidar.StatusCode);
        await CambiarPorcentajeAsync(_anaId, 10m);
        ComisionesMesDto? guardada = await _clienteHttp.GetFromJsonAsync<ComisionesMesDto>(
            "/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=3");
        Assert.NotNull(guardada);
        ComisionVendedorDto filaGuardada = Assert.Single(guardada.Filas);
        Assert.Equal(5m, filaGuardada.Porcentaje);
        Assert.Equal(5.01m, filaGuardada.Comision);
        Assert.Equal(1, filaGuardada.Ordenes);
    }

    [Fact]
    public async Task ReasignarOrdenMueveLaVentaAlNuevoVendedor()
    {
        await AgregarAsignacionAsync(4, _anaId);
        HttpResponseMessage reasignar = await _clienteHttp.PutAsJsonAsync(
            "/api/v1/comisiones-vendedores/ordenes/4/vendedor",
            new AsignarVendedorRequestDto { VendedorId = _luisId });
        Assert.Equal(HttpStatusCode.OK, reasignar.StatusCode);
        ComisionesMesDto? mensual = await _clienteHttp.GetFromJsonAsync<ComisionesMesDto>(
            "/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=3");
        Assert.NotNull(mensual);
        ComisionVendedorDto fila = Assert.Single(mensual.Filas);
        Assert.Equal(_luisId, fila.VendedorId);
        Assert.Equal(4000m, fila.Ventas);
    }

    [Fact]
    public async Task LiquidacionCreaTodasLasFilasEnUnaOperacionYFiltraOrdenes()
    {
        await AgregarAsignacionAsync(1, _anaId);
        await AgregarAsignacionAsync(5, _luisId);
        PaginaOrdenesVendedorDto? sinAsignar = await _clienteHttp.GetFromJsonAsync<PaginaOrdenesVendedorDto>(
            "/api/v1/comisiones-vendedores/ordenes?sinAsignar=true");
        Assert.NotNull(sinAsignar);
        Assert.Equal(await ContarOrdenesAsync() - 2, sinAsignar.Total);
        Assert.DoesNotContain(sinAsignar.Items, item => item.VendedorId.HasValue);
        PaginaOrdenesVendedorDto? ordenesDeEnero = await _clienteHttp.GetFromJsonAsync<PaginaOrdenesVendedorDto>(
            "/api/v1/comisiones-vendedores/ordenes?anio=2026&mes=1");
        Assert.NotNull(ordenesDeEnero);
        Assert.Contains(ordenesDeEnero.Items, item => item.OrdenCompraId == 1);
        Assert.Contains(ordenesDeEnero.Items, item => item.OrdenCompraId == 5);
        Assert.All(ordenesDeEnero.Items, item => Assert.Equal(1, item.Fecha.Month));
        PaginaOrdenesVendedorDto? ordenesDeAbril = await _clienteHttp.GetFromJsonAsync<PaginaOrdenesVendedorDto>(
            "/api/v1/comisiones-vendedores/ordenes?anio=2026&mes=4");
        Assert.NotNull(ordenesDeAbril);
        Assert.Contains(ordenesDeAbril.Items, item => item.OrdenCompraId == 7);
        Assert.All(ordenesDeAbril.Items, item => Assert.Equal(4, item.Fecha.Month));

        HttpResponseMessage liquidar = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/comisiones-vendedores/liquidaciones",
            new LiquidarMesRequestDto { Anio = 2026, Mes = 1 });
        Assert.Equal(HttpStatusCode.OK, liquidar.StatusCode);
        ComisionesMesDto? mensual = await liquidar.Content.ReadFromJsonAsync<ComisionesMesDto>();
        Assert.NotNull(mensual);
        Assert.Equal(2, mensual.Filas.Count);
        Assert.Equal(2500m, mensual.TotalVentas);
        Assert.Equal(115m, mensual.TotalComision);
        Assert.Equal(2, await ContarLiquidacionesAsync());
    }

    [Fact]
    public async Task OpenApiPublicaLasOnceOperacionesDelModulo()
    {
        JsonDocument? documento = await _clienteHttp.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");
        Assert.NotNull(documento);
        JsonElement rutas = documento.RootElement.GetProperty("paths");
        string[] esperadas =
        {
            "/api/v1/comisiones-vendedores/vendedores",
            "/api/v1/comisiones-vendedores/vendedores/{id}",
            "/api/v1/comisiones-vendedores/ordenes",
            "/api/v1/comisiones-vendedores/ordenes/{ordenCompraId}/vendedor",
            "/api/v1/comisiones-vendedores/comisiones",
            "/api/v1/comisiones-vendedores/liquidaciones",
            "/api/v1/comisiones-vendedores/liquidaciones/{id}/pagar"
        };
        Assert.All(esperadas, ruta => Assert.True(rutas.TryGetProperty(ruta, out _), ruta));
    }

    private void RestablecerDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.LiquidacionesComisiones.RemoveRange(contexto.LiquidacionesComisiones);
        contexto.AsignacionesVentas.RemoveRange(contexto.AsignacionesVentas);
        contexto.OrdenesCompra.RemoveRange(contexto.OrdenesCompra.Where(orden => orden.NumeroOrden.StartsWith("OC-TEST-")));
        contexto.Vendedores.RemoveRange(contexto.Vendedores);
        contexto.SaveChanges();
        Vendedor ana = new Vendedor
        {
            Codigo = "V-ANA",
            Nombre = "Ana Torres",
            Email = "ana@example.test",
            PorcentajeComision = 5m,
            Activo = true
        };
        Vendedor luis = new Vendedor
        {
            Codigo = "V-LUIS",
            Nombre = "Luis Rojas",
            PorcentajeComision = 3m,
            Activo = true
        };
        Vendedor inactivo = new Vendedor
        {
            Codigo = "V-OLD",
            Nombre = "Vendedor Antiguo",
            PorcentajeComision = 4m,
            Activo = false
        };
        contexto.Vendedores.AddRange(ana, luis, inactivo);
        contexto.SaveChanges();
        _anaId = ana.Id;
        _luisId = luis.Id;
        _inactivoId = inactivo.Id;
    }

    private async Task<int> ContarVendedoresAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Vendedores.CountAsync();
    }

    private async Task<int> ContarOrdenesAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.OrdenesCompra.CountAsync();
    }

    private async Task<int> ContarAsignacionesAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.AsignacionesVentas.CountAsync();
    }

    private async Task<int> ContarLiquidacionesAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.LiquidacionesComisiones.CountAsync();
    }

    private async Task AgregarAsignacionAsync(int ordenId, int vendedorId)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.AsignacionesVentas.Add(new AsignacionVenta
        {
            OrdenCompraId = ordenId,
            VendedorId = vendedorId,
            FechaAsignacion = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync();
    }

    private async Task AgregarLiquidacionAsync(int vendedorId, int anio, int mes, string estado)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.LiquidacionesComisiones.Add(new LiquidacionComision
        {
            VendedorId = vendedorId,
            Anio = anio,
            Mes = mes,
            Ordenes = 1,
            Ventas = 100m,
            Porcentaje = 5m,
            Comision = 5m,
            Estado = estado,
            FechaCalculo = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync();
    }

    private async Task AgregarOrdenAsync(int id, DateTime fecha, decimal total, int clienteId)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.OrdenesCompra.Add(new OrdenCompra
        {
            Id = id,
            NumeroOrden = $"OC-TEST-{id}",
            Fecha = fecha,
            Total = total,
            ClienteId = clienteId
        });
        await contexto.SaveChangesAsync();
    }

    private async Task CambiarPorcentajeAsync(int vendedorId, decimal porcentaje)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Vendedor vendedor = await contexto.Vendedores.SingleAsync(entidad => entidad.Id == vendedorId);
        vendedor.PorcentajeComision = porcentaje;
        await contexto.SaveChangesAsync();
    }

    private static GuardarVendedorRequestDto Solicitud(string codigo, string nombre, decimal porcentaje)
    {
        return new GuardarVendedorRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            PorcentajeComision = porcentaje,
            Activo = true
        };
    }

    private static HttpRequestMessage CrearSolicitudHttp(string verbo, string ruta, string json)
    {
        HttpRequestMessage solicitud = new HttpRequestMessage(new HttpMethod(verbo), ruta);
        if (json.Length > 0)
        {
            solicitud.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        return solicitud;
    }

    private static async Task AfirmarErrorAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode codigo,
        string mensaje)
    {
        Assert.Equal(codigo, respuesta.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal((int)codigo, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
