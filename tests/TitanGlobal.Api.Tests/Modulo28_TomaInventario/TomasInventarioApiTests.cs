using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo28_TomaInventario;

[Collection("Modulo04")]
public class TomasInventarioApiTests
{
    private const string Ruta = "/api/v1/tomas-inventario";

    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public TomasInventarioApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task CrearCalculaStockSistemaYDiferenciasDesdeElKardex()
    {
        CrearTomaInventarioRequestDto solicitud = new CrearTomaInventarioRequestDto
        {
            Codigo = "  ti-2026-001  ",
            AlmacenId = 1,
            Responsable = "  Ana Torres  ",
            Observaciones = "  Conteo ciclico mensual  ",
            Lineas = new List<LineaTomaInventarioRequestDto>
            {
                new LineaTomaInventarioRequestDto
                {
                    ProductoId = 1,
                    CantidadContada = 18,
                    Observacion = "  Faltan 2 unidades  "
                },
                new LineaTomaInventarioRequestDto
                {
                    ProductoId = 2,
                    CantidadContada = 5
                }
            }
        };

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        TomaInventarioDetalleDto? creada = await respuesta.Content.ReadFromJsonAsync<TomaInventarioDetalleDto>();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Equal("TI-2026-001", creada.Codigo);
        Assert.Equal("Abierta", creada.Estado);
        Assert.Equal("Ana Torres", creada.Responsable);
        Assert.Equal("Conteo ciclico mensual", creada.Observaciones);
        Assert.Equal("ALM-TI1", creada.AlmacenCodigo);
        Assert.Equal(2, creada.Lineas.Count);

        DetalleTomaInventarioDto linea1 = Assert.Single(creada.Lineas, linea => linea.ProductoId == 1);
        Assert.Equal(20, linea1.StockSistema);
        Assert.Equal(18, linea1.CantidadContada);
        Assert.Equal(-2, linea1.Diferencia);
        Assert.Equal("Faltan 2 unidades", linea1.Observacion);

        DetalleTomaInventarioDto linea2 = Assert.Single(creada.Lineas, linea => linea.ProductoId == 2);
        Assert.Equal(3, linea2.StockSistema);
        Assert.Equal(5, linea2.CantidadContada);
        Assert.Equal(2, linea2.Diferencia);
    }

    [Fact]
    public async Task CrearConCodigoDuplicadoDevuelveConflicto()
    {
        await CrearTomaAsync("TI-2026-010", 1, "Carlos Ruiz", 1, 20);

        CrearTomaInventarioRequestDto duplicada = CrearSolicitud("ti-2026-010", 1, "Luis Mora", 1, 19);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, duplicada);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe una toma de inventario con el código TI-2026-010.");
    }

    [Fact]
    public async Task CrearConAlmacenInactivoDevuelveConflicto()
    {
        CrearTomaInventarioRequestDto solicitud = CrearSolicitud("TI-2026-011", 3, "Ana Torres", 1, 10);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "El almacén ALM-TI3 está inactivo.");
    }

    [Fact]
    public async Task CrearConProductoDuplicadoOSinLineasDevuelveBadRequest()
    {
        CrearTomaInventarioRequestDto sinLineas = new CrearTomaInventarioRequestDto
        {
            Codigo = "TI-2026-012",
            AlmacenId = 1,
            Responsable = "Ana Torres"
        };

        HttpResponseMessage respuestaSinLineas = await _clienteHttp.PostAsJsonAsync(Ruta, sinLineas);
        await AfirmarErrorNegocioAsync(
            respuestaSinLineas,
            HttpStatusCode.BadRequest,
            400,
            "La toma de inventario debe incluir al menos un producto.");

        CrearTomaInventarioRequestDto duplicado = new CrearTomaInventarioRequestDto
        {
            Codigo = "TI-2026-013",
            AlmacenId = 1,
            Responsable = "Ana Torres",
            Lineas = new List<LineaTomaInventarioRequestDto>
            {
                new LineaTomaInventarioRequestDto { ProductoId = 1, CantidadContada = 20 },
                new LineaTomaInventarioRequestDto { ProductoId = 1, CantidadContada = 19 }
            }
        };

        HttpResponseMessage respuestaDuplicado = await _clienteHttp.PostAsJsonAsync(Ruta, duplicado);
        await AfirmarErrorNegocioAsync(
            respuestaDuplicado,
            HttpStatusCode.BadRequest,
            400,
            "El producto 1 está duplicado en la toma de inventario.");
    }

    [Fact]
    public async Task ActualizarModificaLineasYRecalculaDiferenciasMientrasEstaAbierta()
    {
        TomaInventarioDetalleDto creada = await CrearTomaAsync("TI-2026-020", 1, "Ana Torres", 1, 18);

        ActualizarTomaInventarioRequestDto actualizacion = new ActualizarTomaInventarioRequestDto
        {
            Responsable = "Pedro Gomez",
            Observaciones = "Reconteo verificado",
            Lineas = new List<LineaTomaInventarioRequestDto>
            {
                new LineaTomaInventarioRequestDto { ProductoId = 1, CantidadContada = 20 },
                new LineaTomaInventarioRequestDto { ProductoId = 2, CantidadContada = 2, Observacion = "Merma" }
            }
        };

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(Ruta + "/" + creada.Id, actualizacion);
        TomaInventarioDetalleDto? actualizada = await respuesta.Content.ReadFromJsonAsync<TomaInventarioDetalleDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(actualizada);
        Assert.Equal("Pedro Gomez", actualizada.Responsable);
        Assert.Equal("Reconteo verificado", actualizada.Observaciones);
        Assert.Equal(2, actualizada.Lineas.Count);
        Assert.Equal(0, Assert.Single(actualizada.Lineas, l => l.ProductoId == 1).Diferencia);
        Assert.Equal(-1, Assert.Single(actualizada.Lineas, l => l.ProductoId == 2).Diferencia);
    }

    [Fact]
    public async Task CerrarYAnularCambianEstadoYBloqueanEdicionesPosteriores()
    {
        TomaInventarioDetalleDto toma1 = await CrearTomaAsync("TI-2026-030", 1, "Ana Torres", 1, 19);
        TomaInventarioDetalleDto toma2 = await CrearTomaAsync("TI-2026-031", 2, "Carlos Ruiz", 1, 0);

        HttpResponseMessage respCerrar = await _clienteHttp.PostAsync(Ruta + "/" + toma1.Id + "/cerrar", null);
        TomaInventarioDetalleDto? cerrada = await respCerrar.Content.ReadFromJsonAsync<TomaInventarioDetalleDto>();
        Assert.Equal(HttpStatusCode.OK, respCerrar.StatusCode);
        Assert.NotNull(cerrada);
        Assert.Equal("Cerrada", cerrada.Estado);

        HttpResponseMessage respAnular = await _clienteHttp.PostAsync(Ruta + "/" + toma2.Id + "/anular", null);
        TomaInventarioDetalleDto? anulada = await respAnular.Content.ReadFromJsonAsync<TomaInventarioDetalleDto>();
        Assert.Equal(HttpStatusCode.OK, respAnular.StatusCode);
        Assert.NotNull(anulada);
        Assert.Equal("Anulada", anulada.Estado);

        ActualizarTomaInventarioRequestDto intentoEditar = new ActualizarTomaInventarioRequestDto
        {
            Responsable = "Otro",
            Lineas = new List<LineaTomaInventarioRequestDto>
            {
                new LineaTomaInventarioRequestDto { ProductoId = 1, CantidadContada = 20 }
            }
        };
        HttpResponseMessage respEditarCerrada = await _clienteHttp.PutAsJsonAsync(Ruta + "/" + toma1.Id, intentoEditar);
        await AfirmarErrorNegocioAsync(
            respEditarCerrada,
            HttpStatusCode.Conflict,
            409,
            "La toma de inventario TI-2026-030 está cerrada y no permite esta operación.");

        HttpResponseMessage respCerrarAnulada = await _clienteHttp.PostAsync(Ruta + "/" + toma2.Id + "/cerrar", null);
        await AfirmarErrorNegocioAsync(
            respCerrarAnulada,
            HttpStatusCode.Conflict,
            409,
            "La toma de inventario TI-2026-031 está anulada y no permite esta operación.");
    }

    [Fact]
    public async Task ListarYResumenFiltranYCalculanIndicadores()
    {
        TomaInventarioDetalleDto abierta = await CrearTomaAsync("TI-2026-040", 1, "Ana Torres", 1, 18);
        TomaInventarioDetalleDto cerrada = await CrearTomaAsync("TI-2026-041", 2, "Carlos Ruiz", 1, 0);
        await _clienteHttp.PostAsync(Ruta + "/" + cerrada.Id + "/cerrar", null);

        PaginaTomasInventarioDto? todas = await _clienteHttp.GetFromJsonAsync<PaginaTomasInventarioDto>(Ruta);
        PaginaTomasInventarioDto? soloCerradas = await _clienteHttp.GetFromJsonAsync<PaginaTomasInventarioDto>(
            Ruta + "?estado=Cerrada");
        PaginaTomasInventarioDto? porBusqueda = await _clienteHttp.GetFromJsonAsync<PaginaTomasInventarioDto>(
            Ruta + "?busqueda=ana");
        ResumenTomasInventarioDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenTomasInventarioDto>(
            Ruta + "/resumen");

        Assert.NotNull(todas);
        Assert.Equal(new[] { cerrada.Id, abierta.Id }, todas.Items.Select(item => item.Id).ToArray());
        Assert.NotNull(soloCerradas);
        Assert.Equal(cerrada.Id, Assert.Single(soloCerradas.Items).Id);
        Assert.NotNull(porBusqueda);
        Assert.Equal(abierta.Id, Assert.Single(porBusqueda.Items).Id);
        Assert.NotNull(resumen);
        Assert.Equal(2, resumen.TotalTomas);
        Assert.Equal(1, resumen.Abiertas);
        Assert.Equal(1, resumen.Cerradas);
        Assert.Equal(0, resumen.Anuladas);
        Assert.Equal(1, resumen.LineasConDiferencia);
    }

    [Fact]
    public async Task ObtenerTomaInexistenteDevuelveNotFound()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(Ruta + "/999");

        await AfirmarErrorNegocioAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "La toma de inventario 999 no existe.");
    }

    private static CrearTomaInventarioRequestDto CrearSolicitud(
        string codigo,
        int almacenId,
        string responsable,
        int productoId,
        int cantidadContada)
    {
        return new CrearTomaInventarioRequestDto
        {
            Codigo = codigo,
            AlmacenId = almacenId,
            Responsable = responsable,
            Lineas = new List<LineaTomaInventarioRequestDto>
            {
                new LineaTomaInventarioRequestDto
                {
                    ProductoId = productoId,
                    CantidadContada = cantidadContada
                }
            }
        };
    }

    private async Task<TomaInventarioDetalleDto> CrearTomaAsync(
        string codigo,
        int almacenId,
        string responsable,
        int productoId,
        int cantidadContada)
    {
        CrearTomaInventarioRequestDto solicitud = CrearSolicitud(codigo, almacenId, responsable, productoId, cantidadContada);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(Ruta, solicitud);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        TomaInventarioDetalleDto? creada = await respuesta.Content.ReadFromJsonAsync<TomaInventarioDetalleDto>();
        Assert.NotNull(creada);
        return creada;
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DetallesTomaInventario.RemoveRange(contexto.DetallesTomaInventario);
        contexto.TomasInventario.RemoveRange(contexto.TomasInventario);
        contexto.MovimientosKardex.RemoveRange(contexto.MovimientosKardex);
        contexto.TransferenciasAlmacen.RemoveRange(contexto.TransferenciasAlmacen);
        contexto.UnidadesSerie.RemoveRange(contexto.UnidadesSerie);
        contexto.Lotes.RemoveRange(contexto.Lotes);
        contexto.ZonasAlmacenaje.RemoveRange(contexto.ZonasAlmacenaje);
        contexto.Almacenes.RemoveRange(contexto.Almacenes);
        contexto.Almacenes.AddRange(
            CrearAlmacen(1, "ALM-TI1", true),
            CrearAlmacen(2, "ALM-TI2", true),
            CrearAlmacen(3, "ALM-TI3", false));
        contexto.MovimientosKardex.AddRange(
            CrearMovimiento(1, 1, 1, "Entrada", 20, 5m, 10),
            CrearMovimiento(2, 2, 1, "Entrada", 3, 100m, 8));
        contexto.SaveChanges();
    }

    private static Almacen CrearAlmacen(int id, string codigo, bool activo)
    {
        return new Almacen
        {
            Id = id,
            Codigo = codigo,
            Nombre = "Almacén " + codigo,
            Tipo = "Central",
            Direccion = "Av. Inventario 1",
            Ciudad = "Lima",
            CapacidadM3 = 100m,
            Responsable = "QA",
            Activo = activo
        };
    }

    private static MovimientoKardex CrearMovimiento(
        int id,
        int productoId,
        int almacenId,
        string tipo,
        int cantidad,
        decimal? costoUnitario,
        int diasAtras)
    {
        return new MovimientoKardex
        {
            Id = id,
            ProductoId = productoId,
            AlmacenId = almacenId,
            Tipo = tipo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Fecha = DateTime.UtcNow.Date.AddDays(-diasAtras)
        };
    }

    private static async Task AfirmarErrorNegocioAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode codigoHttp,
        int estado,
        string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal(codigoHttp, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
