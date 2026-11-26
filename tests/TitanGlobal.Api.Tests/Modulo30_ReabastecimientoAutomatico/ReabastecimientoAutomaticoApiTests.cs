using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo30_ReabastecimientoAutomatico;

[Collection("Modulo04")]
public class ReabastecimientoAutomaticoApiTests
{
    private const string RutaBase = "/api/v1/reabastecimiento-automatico";

    private readonly HttpClient _cliente;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ReabastecimientoAutomaticoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatosModulo30();
    }

    [Fact]
    public async Task ListarReglas_FiltraYRespetaPaginacion()
    {
        PaginaReglasReabastecimientoDto? pagina = await _cliente.GetFromJsonAsync<PaginaReglasReabastecimientoDto>(
            $"{RutaBase}/reglas");

        Assert.NotNull(pagina);
        Assert.Equal(3, pagina.Total);
        Assert.Equal(new[] { "REB-001", "REB-002", "REB-003" }, pagina.Items.Select(item => item.Codigo).ToArray());

        PaginaReglasReabastecimientoDto? enAlerta = await _cliente.GetFromJsonAsync<PaginaReglasReabastecimientoDto>(
            $"{RutaBase}/reglas?soloBajoPuntoReorden=true");

        Assert.NotNull(enAlerta);
        Assert.Single(enAlerta.Items);
        Assert.Equal("REB-001", enAlerta.Items[0].Codigo);
        Assert.True(enAlerta.Items[0].RequiereReabastecimiento);
        Assert.Equal(40, enAlerta.Items[0].CantidadSugeridaActual);

        HttpResponseMessage respuestaInvalida = await _cliente.GetAsync($"{RutaBase}/reglas?pagina=0");
        Assert.Equal(HttpStatusCode.BadRequest, respuestaInvalida.StatusCode);
    }

    [Fact]
    public async Task CrearObtenerYActualizarRegla_CalculaSugerenciaYValidaDuplicados()
    {
        CrearReglaReabastecimientoRequestDto solicitud = new CrearReglaReabastecimientoRequestDto
        {
            Codigo = "  REB-010  ",
            ProductoId = 4,
            AlmacenId = 1,
            ProveedorPreferidoId = 1,
            StockActual = 12,
            StockMinimo = 10,
            PuntoReorden = 20,
            StockMaximo = 55,
            CantidadLoteCompra = 15,
            Activa = true,
            Observaciones = "Regla de prueba para teclado mecánico"
        };

        HttpResponseMessage respuestaCrear = await _cliente.PostAsJsonAsync($"{RutaBase}/reglas", solicitud);
        Assert.Equal(HttpStatusCode.Created, respuestaCrear.StatusCode);

        ReglaReabastecimientoDetalleDto? creada = await respuestaCrear.Content.ReadFromJsonAsync<ReglaReabastecimientoDetalleDto>();
        Assert.NotNull(creada);
        Assert.Equal("REB-010", creada.Codigo);
        Assert.True(creada.RequiereReabastecimiento);
        // StockMaximo (55) - StockActual (12) = 43 -> redondeado a múltiplo de 15 = 45
        Assert.Equal(45, creada.CantidadSugeridaActual);

        ReglaReabastecimientoDetalleDto? detalle = await _cliente.GetFromJsonAsync<ReglaReabastecimientoDetalleDto>(
            $"{RutaBase}/reglas/{creada.Id}");
        Assert.NotNull(detalle);
        Assert.Equal("Teclado Mecánico", detalle.ProductoNombre);

        ActualizarReglaReabastecimientoRequestDto actualizacion = new ActualizarReglaReabastecimientoRequestDto
        {
            ProveedorPreferidoId = 2,
            StockActual = 30,
            StockMinimo = 10,
            PuntoReorden = 20,
            StockMaximo = 60,
            CantidadLoteCompra = 15,
            Activa = true,
            Observaciones = "Stock repuesto"
        };

        HttpResponseMessage respuestaPut = await _cliente.PutAsJsonAsync($"{RutaBase}/reglas/{creada.Id}", actualizacion);
        Assert.Equal(HttpStatusCode.OK, respuestaPut.StatusCode);
        ReglaReabastecimientoListadoDto? actualizada = await respuestaPut.Content.ReadFromJsonAsync<ReglaReabastecimientoListadoDto>();
        Assert.NotNull(actualizada);
        Assert.False(actualizada.RequiereReabastecimiento);
        Assert.Equal(0, actualizada.CantidadSugeridaActual);

        // Código duplicado sin importar mayúsculas -> 409
        HttpResponseMessage respuestaDuplicada = await _cliente.PostAsJsonAsync($"{RutaBase}/reglas", solicitud);
        Assert.Equal(HttpStatusCode.Conflict, respuestaDuplicada.StatusCode);

        // Combinación ProductoId + AlmacenId duplicada -> 409
        CrearReglaReabastecimientoRequestDto mismaCombinacion = new CrearReglaReabastecimientoRequestDto
        {
            Codigo = "REB-099",
            ProductoId = 4,
            AlmacenId = 1,
            StockActual = 5,
            StockMinimo = 5,
            PuntoReorden = 10,
            StockMaximo = 30,
            CantidadLoteCompra = 5,
            Activa = true
        };
        HttpResponseMessage respuestaComb = await _cliente.PostAsJsonAsync($"{RutaBase}/reglas", mismaCombinacion);
        Assert.Equal(HttpStatusCode.Conflict, respuestaComb.StatusCode);
    }

    [Fact]
    public async Task EvaluarGenerarSolicitudYDescartar_FlujoCompleto()
    {
        // 1. Evaluar todas las reglas activas: actualiza la sugerencia pendiente de REB-001 sin duplicar
        HttpResponseMessage respuestaEvaluar = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/evaluar",
            new EvaluarReabastecimientoRequestDto());
        Assert.Equal(HttpStatusCode.OK, respuestaEvaluar.StatusCode);

        ResultadoEvaluacionReabastecimientoDto? evaluacion =
            await respuestaEvaluar.Content.ReadFromJsonAsync<ResultadoEvaluacionReabastecimientoDto>();
        Assert.NotNull(evaluacion);
        Assert.Equal(2, evaluacion.ReglasEvaluadas);
        Assert.Equal(1, evaluacion.ReglasBajoPuntoReorden);
        Assert.Equal(0, evaluacion.SugerenciasGeneradas);
        Assert.Equal(1, evaluacion.SugerenciasActualizadas);

        // 2. Generar SolicitudCompra desde la sugerencia pendiente (Id = 1)
        HttpResponseMessage respuestaGenerar = await _cliente.PostAsync(
            $"{RutaBase}/sugerencias/1/generar-solicitud",
            null);
        Assert.Equal(HttpStatusCode.OK, respuestaGenerar.StatusCode);

        EjecucionReabastecimientoItemDto? procesada =
            await respuestaGenerar.Content.ReadFromJsonAsync<EjecucionReabastecimientoItemDto>();
        Assert.NotNull(procesada);
        Assert.Equal("SolicitudGenerada", procesada.Estado);
        Assert.NotNull(procesada.SolicitudCompraId);
        Assert.StartsWith("SC-REB-", procesada.SolicitudCompraNumero);

        // Reintentar generar solicitud sobre la misma sugerencia -> 409
        HttpResponseMessage respuestaRepetida = await _cliente.PostAsync(
            $"{RutaBase}/sugerencias/1/generar-solicitud",
            null);
        Assert.Equal(HttpStatusCode.Conflict, respuestaRepetida.StatusCode);

        // 3. Nueva evaluación tras procesar la sugerencia anterior crea una nueva sugerencia pendiente
        HttpResponseMessage respuestaEvaluar2 = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/evaluar",
            new EvaluarReabastecimientoRequestDto { ReglaId = 1 });
        ResultadoEvaluacionReabastecimientoDto? evaluacion2 =
            await respuestaEvaluar2.Content.ReadFromJsonAsync<ResultadoEvaluacionReabastecimientoDto>();
        Assert.NotNull(evaluacion2);
        Assert.Equal(1, evaluacion2.SugerenciasGeneradas);
        int nuevaSugerenciaId = evaluacion2.Items[0].Id;

        // 4. Descartar esa nueva sugerencia pendiente
        HttpResponseMessage respuestaDescartar = await _cliente.PostAsJsonAsync(
            $"{RutaBase}/sugerencias/{nuevaSugerenciaId}/descartar",
            new DescartarSugerenciaRequestDto { Motivo = "Compra ya cubierta por transferencia interna." });
        Assert.Equal(HttpStatusCode.OK, respuestaDescartar.StatusCode);
        EjecucionReabastecimientoItemDto? descartada =
            await respuestaDescartar.Content.ReadFromJsonAsync<EjecucionReabastecimientoItemDto>();
        Assert.NotNull(descartada);
        Assert.Equal("Descartada", descartada.Estado);

        // 5. Resumen KPIs
        ResumenReabastecimientoDto? resumen = await _cliente.GetFromJsonAsync<ResumenReabastecimientoDto>(
            $"{RutaBase}/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.TotalReglas);
        Assert.Equal(2, resumen.ReglasActivas);
        Assert.Equal(1, resumen.ReglasEnAlerta);
        Assert.Equal(0, resumen.SugerenciasPendientes);
        Assert.Equal(1, resumen.SolicitudesGeneradas);
    }

    [Fact]
    public async Task CrearRegla_ConUmbralesInvalidosOAlmacenInactivo_DevuelveError()
    {
        CrearReglaReabastecimientoRequestDto umbralInvalido = new CrearReglaReabastecimientoRequestDto
        {
            Codigo = "REB-ERR-1",
            ProductoId = 5,
            AlmacenId = 1,
            StockActual = 10,
            StockMinimo = 20,
            PuntoReorden = 15,
            StockMaximo = 40,
            CantidadLoteCompra = 5,
            Activa = true
        };
        HttpResponseMessage resp400 = await _cliente.PostAsJsonAsync($"{RutaBase}/reglas", umbralInvalido);
        Assert.Equal(HttpStatusCode.BadRequest, resp400.StatusCode);

        // Almacén 4 (ALM-TRU-01) está inactivo en semilla -> 409
        CrearReglaReabastecimientoRequestDto almacenInactivo = new CrearReglaReabastecimientoRequestDto
        {
            Codigo = "REB-ERR-2",
            ProductoId = 5,
            AlmacenId = 4,
            StockActual = 5,
            StockMinimo = 5,
            PuntoReorden = 10,
            StockMaximo = 30,
            CantidadLoteCompra = 5,
            Activa = true
        };
        HttpResponseMessage resp409 = await _cliente.PostAsJsonAsync($"{RutaBase}/reglas", almacenInactivo);
        Assert.Equal(HttpStatusCode.Conflict, resp409.StatusCode);

        HttpResponseMessage resp404 = await _cliente.GetAsync($"{RutaBase}/reglas/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp404.StatusCode);
    }

    private void RestablecerDatosModulo30()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        List<EjecucionReabastecimiento> ejecuciones = contexto.EjecucionesReabastecimiento.ToList();
        if (ejecuciones.Count > 0)
        {
            contexto.EjecucionesReabastecimiento.RemoveRange(ejecuciones);
        }

        List<ReglaReabastecimiento> reglas = contexto.ReglasReabastecimiento.ToList();
        if (reglas.Count > 0)
        {
            contexto.ReglasReabastecimiento.RemoveRange(reglas);
        }

        List<SolicitudCompra> solicitudesGeneradas = contexto.SolicitudesCompra
            .Include(item => item.Detalles)
            .Where(item => item.Numero.StartsWith("SC-REB-"))
            .ToList();
        if (solicitudesGeneradas.Count > 0)
        {
            contexto.SolicitudesCompra.RemoveRange(solicitudesGeneradas);
        }

        contexto.SaveChanges();

        if (!contexto.Almacenes.Any(a => a.Id == 1))
        {
            contexto.Almacenes.Add(new Almacen { Id = 1, Codigo = "ALM-LIM-01", Nombre = "Almacén Central Lima", Tipo = "Central", Direccion = "Av. Industrial 100", Ciudad = "Lima", CapacidadM3 = 500m, Responsable = "QA", Activo = true });
        }
        if (!contexto.Almacenes.Any(a => a.Id == 2))
        {
            contexto.Almacenes.Add(new Almacen { Id = 2, Codigo = "ALM-ARE-01", Nombre = "Almacén Regional Arequipa", Tipo = "Regional", Direccion = "Av. Parra 200", Ciudad = "Arequipa", CapacidadM3 = 300m, Responsable = "QA", Activo = true });
        }
        if (!contexto.Almacenes.Any(a => a.Id == 4))
        {
            contexto.Almacenes.Add(new Almacen { Id = 4, Codigo = "ALM-TRU-01", Nombre = "Almacén Trujillo", Tipo = "Regional", Direccion = "Av. América 400", Ciudad = "Trujillo", CapacidadM3 = 200m, Responsable = "QA", Activo = false });
        }
        if (!contexto.Proveedores.Any(pr => pr.Id == 1))
        {
            contexto.Proveedores.Add(new Proveedor { Id = 1, RazonSocial = "Proveedor Alpha S.A.C.", Ruc = "20511111111", Pais = "Perú", Email = "ventas@alpha.pe", Telefono = "999111222", Activo = true });
        }
        if (!contexto.Proveedores.Any(pr => pr.Id == 2))
        {
            contexto.Proveedores.Add(new Proveedor { Id = 2, RazonSocial = "Distribuidora Beta S.R.L.", Ruc = "20522222222", Pais = "Perú", Email = "contacto@beta.pe", Telefono = "999333444", Activo = true });
        }
        contexto.SaveChanges();

        DateTime fechaBase = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        ReglaReabastecimiento regla1 = new ReglaReabastecimiento
        {
            Id = 1,
            Codigo = "REB-001",
            ProductoId = 3,
            AlmacenId = 1,
            ProveedorPreferidoId = 1,
            StockActual = 15,
            StockMinimo = 10,
            PuntoReorden = 25,
            StockMaximo = 50,
            CantidadLoteCompra = 10,
            Activa = true,
            UltimaEvaluacion = fechaBase,
            Observaciones = "Mouse óptico bajo punto de reorden"
        };

        ReglaReabastecimiento regla2 = new ReglaReabastecimiento
        {
            Id = 2,
            Codigo = "REB-002",
            ProductoId = 6,
            AlmacenId = 1,
            ProveedorPreferidoId = 2,
            StockActual = 80,
            StockMinimo = 20,
            PuntoReorden = 40,
            StockMaximo = 120,
            CantidadLoteCompra = 20,
            Activa = true,
            UltimaEvaluacion = fechaBase,
            Observaciones = "Cable HDMI con stock saludable"
        };

        ReglaReabastecimiento regla3 = new ReglaReabastecimiento
        {
            Id = 3,
            Codigo = "REB-003",
            ProductoId = 1,
            AlmacenId = 2,
            ProveedorPreferidoId = 1,
            StockActual = 2,
            StockMinimo = 5,
            PuntoReorden = 10,
            StockMaximo = 25,
            CantidadLoteCompra = 5,
            Activa = false,
            Observaciones = "Regla pausada temporalmente"
        };

        contexto.ReglasReabastecimiento.AddRange(regla1, regla2, regla3);
        contexto.SaveChanges();

        EjecucionReabastecimiento ejecucion1 = new EjecucionReabastecimiento
        {
            Id = 1,
            ReglaReabastecimientoId = 1,
            FechaEjecucion = fechaBase,
            StockActual = 15,
            CantidadSugerida = 40,
            Estado = "Pendiente",
            Observacion = "Sugerencia inicial bajo punto de reorden"
        };

        contexto.EjecucionesReabastecimiento.Add(ejecucion1);
        contexto.SaveChanges();
    }
}
