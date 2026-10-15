using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo78_NotificacionesAlertas.Dtos;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;

namespace TitanGlobal.Api.Tests.Modulo78_NotificacionesAlertas;

[Collection("Modulo04")]
public class AlertasApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public AlertasApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        PrepararDatos();
    }

    [Fact]
    public async Task GeneraLosSieteTiposConMensajesYLimitesCoincidentesConSusModulos()
    {
        PaginaAlertasDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaAlertasDto>(
            "/api/v1/alertas?pagina=1&tamanoPagina=100");

        Assert.NotNull(pagina);
        Assert.Equal(7, pagina.Total);
        Assert.Equal(new[]
        {
            "orden-atrasada:1", "reclamo-vencido:1", "contrato-excedido:1", "almacen-lleno:1",
            "contrato-por-vencer:3", "solicitud-pendiente:1", "cotizacion-vencida:1"
        }, pagina.Items.Select(alerta => alerta.Clave).ToArray());
        Assert.Equal(7, pagina.Items.Select(alerta => alerta.Tipo).Distinct().Count());
        Assert.Contains(pagina.Items, alerta => alerta.Tipo == "ReclamoVencido");
        Assert.Contains(pagina.Items, alerta => alerta.Tipo == "OrdenAbastecimientoAtrasada");
        Assert.Contains(pagina.Items, alerta => alerta.Tipo == "ContratoExcedido");
        Assert.Contains(pagina.Items, alerta => alerta.Tipo == "AlmacenCasiLleno");
        Assert.Contains(pagina.Items, alerta => alerta.Tipo == "ContratoPorVencer");
        Assert.Contains(pagina.Items, alerta => alerta.Tipo == "CotizacionVencida");
        Assert.Contains(pagina.Items, alerta => alerta.Tipo == "SolicitudPendienteAntigua");
        Assert.Equal("El reclamo REC-TEST de Acme Industrial superó su plazo de atención.",
            Obtener(pagina, "ReclamoVencido").Mensaje);
        Assert.Equal("La orden OA-TEST de Proveedor Test está atrasada desde el "
            + DateTime.UtcNow.Date.AddDays(-1).ToString("dd/MM/yyyy") + ".",
            Obtener(pagina, "OrdenAbastecimientoAtrasada").Mensaje);
        Assert.Equal("El contrato CC-EXCEDIDO de Cliente sin órdenes va al 200.00 % de su monto comprometido.",
            Obtener(pagina, "ContratoExcedido").Mensaje);
        Assert.Equal("El almacén ALM-TEST tiene asignado el 90.00 % de su capacidad.",
            Obtener(pagina, "AlmacenCasiLleno").Mensaje);
        Assert.Equal("El contrato CC-POR-VENCER de Beta Comercio termina el "
            + DateTime.UtcNow.Date.AddDays(10).ToString("dd/MM/yyyy") + ".",
            Obtener(pagina, "ContratoPorVencer").Mensaje);
        Assert.Equal("La cotización COT-TEST de Acme Industrial venció sin respuesta.",
            Obtener(pagina, "CotizacionVencida").Mensaje);
        Assert.Equal("La solicitud SC-TEST lleva 8 días pendiente.",
            Obtener(pagina, "SolicitudPendienteAntigua").Mensaje);
        Assert.Equal(0, pagina.Items.Count(alerta => alerta.Tipo == "ReclamoVencido" && alerta.RegistroId == 2));
        Assert.Equal(0, pagina.Items.Count(alerta => alerta.Tipo == "OrdenAbastecimientoAtrasada" && alerta.RegistroId == 2));
        Assert.Equal(0, pagina.Items.Count(alerta => alerta.Tipo == "ContratoExcedido" && alerta.RegistroId == 2));
        Assert.Equal(0, pagina.Items.Count(alerta => alerta.Tipo == "AlmacenCasiLleno" && alerta.RegistroId == 2));
        Assert.Equal(0, pagina.Items.Count(alerta => alerta.Tipo == "ContratoPorVencer" && alerta.RegistroId == 4));
        Assert.Equal(0, pagina.Items.Count(alerta => alerta.Tipo == "CotizacionVencida" && alerta.RegistroId == 2));
        Assert.Equal(0, pagina.Items.Count(alerta => alerta.Tipo == "SolicitudPendienteAntigua" && alerta.RegistroId == 2));
    }

    [Fact]
    public async Task FiltrosResumenPaginacionYOrdenSonExactos()
    {
        PaginaAlertasDto? altas = await _clienteHttp.GetFromJsonAsync<PaginaAlertasDto>(
            "/api/v1/alertas?severidad=aLtA&pagina=1&tamanoPagina=1");
        ResumenAlertasDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenAlertasDto>("/api/v1/alertas/resumen");
        JsonDocument openApi = JsonDocument.Parse(await _clienteHttp.GetStringAsync("/openapi/v1.json"));

        Assert.NotNull(altas);
        Assert.Equal(3, altas.Total);
        Assert.Equal("OrdenAbastecimientoAtrasada", Assert.Single(altas.Items).Tipo);
        Assert.Equal(1, altas.Pagina);
        Assert.Equal(1, altas.TamanoPagina);
        Assert.NotNull(resumen);
        Assert.Equal(7, resumen.Total);
        Assert.Equal(3, resumen.Altas);
        Assert.Equal(2, resumen.Medias);
        Assert.Equal(2, resumen.Bajas);
        Assert.Equal(new[]
        {
            "ReclamoVencido", "OrdenAbastecimientoAtrasada", "ContratoExcedido",
            "AlmacenCasiLleno", "ContratoPorVencer", "CotizacionVencida", "SolicitudPendienteAntigua"
        }, resumen.PorTipo.Select(tipo => tipo.Tipo).ToArray());
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 1, 1 }, resumen.PorTipo.Select(tipo => tipo.Alertas).ToArray());
        JsonElement rutas = openApi.RootElement.GetProperty("paths");
        Assert.True(rutas.TryGetProperty("/api/v1/alertas", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/alertas/resumen", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/alertas/descartar", out _));
        Assert.True(rutas.TryGetProperty("/api/v1/alertas/restaurar", out _));
    }

    [Theory]
    [InlineData("/api/v1/alertas?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("/api/v1/alertas?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("/api/v1/alertas?tipo=Desconocido", "El tipo de alerta no es válido.")]
    [InlineData("/api/v1/alertas?severidad=Critica", "La severidad no es válida.")]
    public async Task FiltrosInvalidosDevuelvenCodigoMensajeLiteralYSinDetail(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);
        JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(400, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }

    [Fact]
    public async Task DescartarRestaurarYVolverADescartarPersistenSoloLaClave()
    {
        const string clave = "reclamo-vencido:1";
        HttpResponseMessage descartada = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/alertas/descartar", new ClaveAlertaRequestDto { Clave = clave });
        PaginaAlertasDto? porDefecto = await _clienteHttp.GetFromJsonAsync<PaginaAlertasDto>("/api/v1/alertas");
        PaginaAlertasDto? incluidas = await _clienteHttp.GetFromJsonAsync<PaginaAlertasDto>(
            "/api/v1/alertas?incluirDescartadas=true");
        HttpResponseMessage segundaVez = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/alertas/descartar", new ClaveAlertaRequestDto { Clave = clave });
        HttpResponseMessage restaurada = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/alertas/restaurar", new ClaveAlertaRequestDto { Clave = clave });
        HttpResponseMessage segundaRestauracion = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/alertas/restaurar", new ClaveAlertaRequestDto { Clave = clave });
        HttpResponseMessage inexistente = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/alertas/descartar", new ClaveAlertaRequestDto { Clave = "reclamo-vencido:999" });

        Assert.Equal(HttpStatusCode.NoContent, descartada.StatusCode);
        Assert.NotNull(porDefecto);
        Assert.DoesNotContain(porDefecto.Items, alerta => alerta.Clave == clave);
        Assert.NotNull(incluidas);
        Assert.Contains(incluidas.Items, alerta => alerta.Clave == clave && alerta.Descartada);
        await AfirmarErrorAsync(segundaVez, 409, "La alerta reclamo-vencido:1 ya está descartada.");
        Assert.Equal(HttpStatusCode.NoContent, restaurada.StatusCode);
        await AfirmarErrorAsync(segundaRestauracion, 404, "La alerta reclamo-vencido:1 no está descartada.");
        await AfirmarErrorAsync(inexistente, 404, "La alerta reclamo-vencido:999 no existe o ya no está activa.");
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Assert.Empty(await contexto.AlertasDescartadas.ToListAsync());
    }

    [Theory]
    [InlineData("", "La clave de la alerta es obligatoria.")]
    [InlineData(" ", "La clave de la alerta es obligatoria.")]
    public async Task ClaveVaciaDevuelveErrorLiteral(string clave, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/alertas/descartar", new ClaveAlertaRequestDto { Clave = clave });

        await AfirmarErrorAsync(respuesta, 400, mensaje);
    }

    [Fact]
    public async Task ClaveMayorACienDevuelveErrorLiteral()
    {
        string clave = new string('x', 101);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/alertas/descartar", new ClaveAlertaRequestDto { Clave = clave });

        await AfirmarErrorAsync(respuesta, 400, "La clave de la alerta no puede superar 100 caracteres.");
    }

    private static AlertaDto Obtener(PaginaAlertasDto pagina, string tipo)
    {
        return Assert.Single(pagina.Items, alerta => alerta.Tipo == tipo);
    }

    private static async Task AfirmarErrorAsync(HttpResponseMessage respuesta, int codigo, string mensaje)
    {
        JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal((HttpStatusCode)codigo, respuesta.StatusCode);
        Assert.Equal(codigo, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }

    private void PrepararDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.AlertasDescartadas.RemoveRange(contexto.AlertasDescartadas);
        contexto.ReclamosPostventa.RemoveRange(contexto.ReclamosPostventa);
        contexto.OrdenesAbastecimiento.RemoveRange(contexto.OrdenesAbastecimiento);
        contexto.ContratosComerciales.RemoveRange(contexto.ContratosComerciales);
        contexto.ZonasAlmacenaje.RemoveRange(contexto.ZonasAlmacenaje);
        contexto.Almacenes.RemoveRange(contexto.Almacenes);
        contexto.Cotizaciones.RemoveRange(contexto.Cotizaciones);
        contexto.SolicitudesCompra.RemoveRange(contexto.SolicitudesCompra);
        contexto.OrdenesCompra.RemoveRange(contexto.OrdenesCompra.Where(orden => orden.Id == 50));
        contexto.Proveedores.RemoveRange(contexto.Proveedores.Where(proveedor => proveedor.Id == 999));
        contexto.SaveChanges();
        contexto.Proveedores.Add(new Proveedor
        {
            Id = 999,
            RazonSocial = "Proveedor Test",
            Ruc = "20699999991",
            Pais = "Peru",
            Activo = true
        });

        DateTime hoy = DateTime.UtcNow.Date;
        contexto.ReclamosPostventa.AddRange(
            new ReclamoPostventa
            {
                Id = 1,
                Numero = "REC-TEST",
                Fecha = hoy.AddDays(-5),
                ClienteId = 1,
                OrdenCompraId = 1,
                Tipo = "Defecto",
                Prioridad = "Alta",
                Descripcion = "Vencido",
                Estado = "Abierto",
                FechaLimite = hoy.AddDays(-1)
            },
            new ReclamoPostventa
            {
                Id = 2,
                Numero = "REC-LIMITE",
                Fecha = hoy,
                ClienteId = 1,
                OrdenCompraId = 1,
                Tipo = "Defecto",
                Prioridad = "Alta",
                Descripcion = "No vencido",
                Estado = "Abierto",
                FechaLimite = hoy.AddDays(1)
            });
        contexto.OrdenesAbastecimiento.AddRange(
            new OrdenAbastecimiento
            {
                Id = 1,
                Numero = "OA-TEST",
                Fecha = hoy.AddDays(-10),
                ProveedorId = 999,
                Estado = "Emitida",
                FechaEntregaEstimada = hoy.AddDays(-1)
            },
            new OrdenAbastecimiento
            {
                Id = 2,
                Numero = "OA-LIMITE",
                Fecha = hoy,
                ProveedorId = 999,
                Estado = "Emitida",
                FechaEntregaEstimada = hoy
            });
        contexto.OrdenesCompra.Add(new OrdenCompra
        {
            Id = 50,
            NumeroOrden = "OC-ALERTA",
            Fecha = hoy.AddDays(-1),
            Total = 200m,
            ClienteId = 5
        });
        contexto.ContratosComerciales.AddRange(
            new ContratoComercial
            {
                Id = 1,
                Numero = "CC-EXCEDIDO",
                ClienteId = 5,
                Titulo = "Excedido",
                FechaInicio = hoy.AddDays(-10),
                FechaFin = hoy.AddDays(60),
                MontoComprometido = 100m,
                CondicionPago = "Contado",
                Estado = "Vigente"
            },
            new ContratoComercial
            {
                Id = 2,
                Numero = "CC-100",
                ClienteId = 5,
                Titulo = "Exactamente cien por ciento",
                FechaInicio = hoy.AddDays(-10),
                FechaFin = hoy.AddDays(60),
                MontoComprometido = 200m,
                CondicionPago = "Contado",
                Estado = "Vigente"
            },
            new ContratoComercial
            {
                Id = 3,
                Numero = "CC-POR-VENCER",
                ClienteId = 2,
                Titulo = "Por vencer",
                FechaInicio = hoy.AddDays(-1),
                FechaFin = hoy.AddDays(10),
                MontoComprometido = 1000m,
                CondicionPago = "Contado",
                Estado = "Vigente"
            },
            new ContratoComercial
            {
                Id = 4,
                Numero = "CC-31-DIAS",
                ClienteId = 3,
                Titulo = "Límite por vencer",
                FechaInicio = hoy,
                FechaFin = hoy.AddDays(31),
                MontoComprometido = 1000m,
                CondicionPago = "Contado",
                Estado = "Vigente"
            });
        contexto.Almacenes.AddRange(
            new Almacen
            {
                Id = 1,
                Codigo = "ALM-TEST",
                Nombre = "Almacén Test",
                Tipo = "Central",
                Direccion = "Dirección",
                Ciudad = "Lima",
                CapacidadM3 = 100m,
                Activo = true
            },
            new Almacen
            {
                Id = 2,
                Codigo = "ALM-89",
                Nombre = "Almacén bajo límite",
                Tipo = "Regional",
                Direccion = "Dirección",
                Ciudad = "Lima",
                CapacidadM3 = 100m,
                Activo = true
            });
        contexto.ZonasAlmacenaje.AddRange(
            new ZonaAlmacenaje
            {
                Id = 1,
                AlmacenId = 1,
                Codigo = "Z-90",
                Nombre = "Zona 90",
                Tipo = "Almacenamiento",
                Temperatura = "Ambiente",
                CapacidadM3 = 90m,
                Activa = true
            },
            new ZonaAlmacenaje
            {
                Id = 2,
                AlmacenId = 2,
                Codigo = "Z-8999",
                Nombre = "Zona 89.99",
                Tipo = "Almacenamiento",
                Temperatura = "Ambiente",
                CapacidadM3 = 89.99m,
                Activa = true
            });
        contexto.Cotizaciones.AddRange(
            new Cotizacion
            {
                Id = 1,
                Numero = "COT-TEST",
                Fecha = hoy.AddDays(-10),
                ClienteId = 1,
                FechaValidez = hoy.AddDays(-1),
                Estado = "Emitida"
            },
            new Cotizacion
            {
                Id = 2,
                Numero = "COT-HOY",
                Fecha = hoy,
                ClienteId = 1,
                FechaValidez = hoy,
                Estado = "Emitida"
            });
        contexto.SolicitudesCompra.AddRange(
            new SolicitudCompra
            {
                Id = 1,
                Numero = "SC-TEST",
                Fecha = hoy.AddDays(-8),
                Solicitante = "Test",
                Estado = "Pendiente"
            },
            new SolicitudCompra
            {
                Id = 2,
                Numero = "SC-7-DIAS",
                Fecha = hoy.AddDays(-7),
                Solicitante = "Test",
                Estado = "Pendiente"
            });
        contexto.SaveChanges();
    }
}
