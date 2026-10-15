using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.Dtos;

namespace TitanGlobal.Api.Tests.Modulo77_AuditoriaTransaccional;

[Collection("Modulo04")]
public class AuditoriaTransaccionalApiTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    private readonly HttpClient _clienteHttp;

    public AuditoriaTransaccionalApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _fabrica.SeedDatabase();
        _clienteHttp = _fabrica.CreateClient();
    }

    [Fact]
    public async Task CapturaAltaModificacionBajaConValoresExactosYOmiteModificacionSinCambios()
    {
        await LimpiarAuditoriaAsync();
        string ruc = CrearRucUnico();
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = "Cliente de auditoría",
            Ruc = ruc,
            Descripcion = "Descripción inicial"
        };
        HttpResponseMessage respuestaAlta = await EnviarClienteAsync(HttpMethod.Post, "/api/v1/clientes", solicitud, " operador ");
        JsonDocument documentoAlta = await JsonDocument.ParseAsync(await respuestaAlta.Content.ReadAsStreamAsync());
        int clienteId = documentoAlta.RootElement.GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, respuestaAlta.StatusCode);
        List<RegistroAuditoria> registrosAlta = await ObtenerAuditoriaAsync();
        RegistroAuditoria alta = Assert.Single(registrosAlta);
        Assert.Equal("Alta", alta.Accion);
        Assert.Equal("Cliente", alta.Entidad);
        Assert.Equal(clienteId.ToString(CultureInfo.InvariantCulture), alta.EntidadId);
        Assert.Equal("operador", alta.Usuario);
        Assert.Equal("POST /api/v1/clientes", alta.Ruta);
        List<CambioCampoDto> camposAlta = DeserializarCambios(alta.Cambios);
        Assert.Equal(4, camposAlta.Count);
        Assert.All(camposAlta, campo => Assert.Null(campo.Antes));
        Assert.Equal(clienteId.ToString(CultureInfo.InvariantCulture), BuscarCampo(camposAlta, "Id").Despues);
        Assert.Equal("Cliente de auditoría", BuscarCampo(camposAlta, "Nombre").Despues);
        Assert.Equal(ruc, BuscarCampo(camposAlta, "Ruc").Despues);
        Assert.Equal("Descripción inicial", BuscarCampo(camposAlta, "Descripcion").Despues);

        GuardarClienteRequestDto solicitudModificada = new GuardarClienteRequestDto
        {
            Nombre = "Cliente actualizado",
            Ruc = ruc,
            Descripcion = "Descripción final"
        };
        HttpResponseMessage respuestaModificacion = await EnviarClienteAsync(
            HttpMethod.Put,
            "/api/v1/clientes/" + clienteId,
            solicitudModificada,
            " operador ");
        Assert.Equal(HttpStatusCode.OK, respuestaModificacion.StatusCode);
        List<RegistroAuditoria> registrosModificados = await ObtenerAuditoriaAsync();
        Assert.Equal(2, registrosModificados.Count);
        RegistroAuditoria modificacion = registrosModificados.Single(registro => registro.Accion == "Modificacion");
        Assert.Equal("PUT /api/v1/clientes/" + clienteId, modificacion.Ruta);
        List<CambioCampoDto> camposModificados = DeserializarCambios(modificacion.Cambios);
        Assert.Equal(new[] { "Descripcion", "Nombre" }, camposModificados.Select(campo => campo.Campo).OrderBy(campo => campo).ToArray());
        Assert.Equal("Cliente de auditoría", BuscarCampo(camposModificados, "Nombre").Antes);
        Assert.Equal("Cliente actualizado", BuscarCampo(camposModificados, "Nombre").Despues);
        Assert.Equal("Descripción inicial", BuscarCampo(camposModificados, "Descripcion").Antes);
        Assert.Equal("Descripción final", BuscarCampo(camposModificados, "Descripcion").Despues);

        HttpResponseMessage respuestaSinCambios = await EnviarClienteAsync(
            HttpMethod.Put,
            "/api/v1/clientes/" + clienteId,
            solicitudModificada,
            " operador ");
        Assert.Equal(HttpStatusCode.OK, respuestaSinCambios.StatusCode);
        Assert.Equal(2, (await ObtenerAuditoriaAsync()).Count);

        HttpResponseMessage respuestaBaja = await _clienteHttp.DeleteAsync("/api/v1/clientes/" + clienteId);
        Assert.Equal(HttpStatusCode.NoContent, respuestaBaja.StatusCode);
        List<RegistroAuditoria> registrosFinales = await ObtenerAuditoriaAsync();
        Assert.Equal(3, registrosFinales.Count);
        RegistroAuditoria baja = registrosFinales.Single(registro => registro.Accion == "Baja");
        Assert.Equal("DELETE /api/v1/clientes/" + clienteId, baja.Ruta);
        List<CambioCampoDto> camposBaja = DeserializarCambios(baja.Cambios);
        Assert.Equal(4, camposBaja.Count);
        Assert.All(camposBaja, campo => Assert.Null(campo.Despues));
        Assert.Equal("Cliente actualizado", BuscarCampo(camposBaja, "Nombre").Antes);
    }

    [Fact]
    public async Task UsuarioPorDefectoEsSistemaYFalloDeNegocioNoGeneraAuditoria()
    {
        await LimpiarAuditoriaAsync();
        GuardarClienteRequestDto solicitudValida = new GuardarClienteRequestDto
        {
            Nombre = "Cliente por defecto",
            Ruc = CrearRucUnico()
        };
        HttpResponseMessage respuestaValida = await _clienteHttp.PostAsJsonAsync("/api/v1/clientes", solicitudValida);
        Assert.Equal(HttpStatusCode.OK, respuestaValida.StatusCode);
        RegistroAuditoria registro = Assert.Single(await ObtenerAuditoriaAsync());
        Assert.Equal("sistema", registro.Usuario);
        Assert.Equal("POST /api/v1/clientes", registro.Ruta);

        long totalAntes = await ContarAuditoriaAsync();
        GuardarClienteRequestDto solicitudDuplicada = new GuardarClienteRequestDto
        {
            Nombre = "RUC duplicado",
            Ruc = "20111111111"
        };
        HttpResponseMessage respuestaDuplicada = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/clientes",
            solicitudDuplicada);
        long totalDespues = await ContarAuditoriaAsync();
        JsonDocument error = await JsonDocument.ParseAsync(await respuestaDuplicada.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Conflict, respuestaDuplicada.StatusCode);
        Assert.Equal("Ya existe un cliente con el RUC 20111111111.", error.RootElement.GetProperty("Message").GetString());
        Assert.Equal(totalAntes, totalDespues);
    }

    [Fact]
    public async Task UsuarioSeRecortaATreintaCaracteresDespuesDeTrim()
    {
        await LimpiarAuditoriaAsync();
        GuardarClienteRequestDto solicitud = new GuardarClienteRequestDto
        {
            Nombre = "Cliente con usuario largo",
            Ruc = CrearRucUnico()
        };
        HttpResponseMessage respuesta = await EnviarClienteAsync(
            HttpMethod.Post,
            "/api/v1/clientes",
            solicitud,
            " 123456789012345678901234567890ABC ");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        RegistroAuditoria registro = Assert.Single(await ObtenerAuditoriaAsync());
        Assert.Equal("123456789012345678901234567890", registro.Usuario);
    }

    [Fact]
    public async Task UnaOperacionQueGuardaVariasEntidadesGeneraUnRegistroPorEntidad()
    {
        await LimpiarAuditoriaAsync();
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Cliente cliente = new Cliente
        {
            Nombre = "Cliente de dos entidades",
            Ruc = CrearRucUnico()
        };
        Producto producto = new Producto
        {
            Nombre = "Producto de dos entidades",
            Categoria = "Prueba",
            Precio = 12.75m
        };
        contexto.AddRange(cliente, producto);
        await contexto.SaveChangesAsync();

        List<RegistroAuditoria> registros = await ObtenerAuditoriaAsync();
        Assert.Equal(2, registros.Count);
        Assert.Equal(new[] { "Cliente", "Producto" }, registros.Select(registro => registro.Entidad).OrderBy(nombre => nombre).ToArray());
        Assert.All(registros, registro => Assert.Null(registro.Ruta));
        Assert.All(registros, registro => Assert.Equal("sistema", registro.Usuario));
        Assert.Contains(registros, registro => registro.Entidad == "Cliente" && registro.EntidadId == cliente.Id.ToString(CultureInfo.InvariantCulture));
        Assert.Contains(registros, registro => registro.Entidad == "Producto" && registro.EntidadId == producto.Id.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ListadoAplicaFiltrosFechasOrdenYTotalConPaginacion()
    {
        await LimpiarAuditoriaAsync();
        DateTime hoy = DateTime.UtcNow.Date;
        await InsertarAuditoriaAsync(
            CrearRegistro("Cliente", "11", "Alta", "Marta", hoy.AddHours(10), "POST /api/v1/clientes", "[]"),
            CrearRegistro("Cliente", "12", "Modificacion", "Marta", hoy.AddHours(11), "PUT /api/v1/clientes/12", "[]"),
            CrearRegistro("Producto", "11", "Baja", "Luis", hoy.AddHours(11), "DELETE /api/v1/productos/11", "[]"));

        PaginaAuditoriaDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaAuditoriaDto>(
            "/api/v1/auditoria?entidad=cliente&entidadId=12&accion=MODIFICACION&usuario=mArTa"
            + "&fechaDesde=" + Uri.EscapeDataString(hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            + "&fechaHasta=" + Uri.EscapeDataString(hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            + "&pagina=1&tamanoPagina=1");
        Assert.NotNull(pagina);
        Assert.Equal(1, pagina.Total);
        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(1, pagina.TamanoPagina);
        Assert.Equal("12", Assert.Single(pagina.Items).EntidadId);

        PaginaAuditoriaDto? orden = await _clienteHttp.GetFromJsonAsync<PaginaAuditoriaDto>("/api/v1/auditoria?pagina=1&tamanoPagina=2");
        Assert.NotNull(orden);
        Assert.Equal(3, orden.Total);
        Assert.Equal(new[] { "Producto", "Cliente" }, orden.Items.Select(item => item.Entidad).ToArray());
        Assert.True(orden.Items[0].Id > orden.Items[1].Id);
    }

    [Fact]
    public async Task AccionYRangoDeFechasInvalidosDevuelvenMensajesLiterales()
    {
        HttpResponseMessage accion = await _clienteHttp.GetAsync("/api/v1/auditoria?accion=Editar");
        JsonDocument errorAccion = await JsonDocument.ParseAsync(await accion.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.BadRequest, accion.StatusCode);
        Assert.Equal(400, errorAccion.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal("La acción no es válida.", errorAccion.RootElement.GetProperty("Message").GetString());
        Assert.False(errorAccion.RootElement.TryGetProperty("Detail", out _));

        HttpResponseMessage fechas = await _clienteHttp.GetAsync(
            "/api/v1/auditoria?fechaDesde=2026-10-15T12%3A00%3A00Z&fechaHasta=2026-10-15T11%3A00%3A00Z");
        JsonDocument errorFechas = await JsonDocument.ParseAsync(await fechas.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.BadRequest, fechas.StatusCode);
        Assert.Equal(400, errorFechas.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(
            "La fecha desde no puede ser posterior a la fecha hasta.",
            errorFechas.RootElement.GetProperty("Message").GetString());
        Assert.False(errorFechas.RootElement.TryGetProperty("Detail", out _));
    }

    [Fact]
    public async Task DetalleEntidadesYOpenApiExponenElContratoDeSoloLectura()
    {
        await LimpiarAuditoriaAsync();
        RegistroAuditoria registro = CrearRegistro(
            "Cliente",
            "42",
            "Modificacion",
            "michael",
            DateTime.UtcNow,
            "PUT /api/v1/clientes/42",
            "[{\"campo\":\"Nombre\",\"antes\":\"A\",\"despues\":\"B\"}]");
        await InsertarAuditoriaAsync(registro);

        RegistroDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<RegistroDetalleDto>(
            "/api/v1/auditoria/" + registro.Id);
        List<string>? entidades = await _clienteHttp.GetFromJsonAsync<List<string>>("/api/v1/auditoria/entidades");
        JsonDocument openApi = await JsonDocument.ParseAsync(
            await _clienteHttp.GetStreamAsync("/openapi/v1.json"));
        JsonElement rutas = openApi.RootElement.GetProperty("paths");

        Assert.NotNull(detalle);
        Assert.Equal("Cliente", detalle.Entidad);
        Assert.Equal(1, detalle.CamposCambiados);
        Assert.Equal("A", Assert.Single(detalle.Cambios).Antes);
        Assert.Equal(new[] { "Cliente" }, entidades);
        Assert.Contains("/api/v1/auditoria", rutas.EnumerateObject().Select(ruta => ruta.Name));
        Assert.Contains("/api/v1/auditoria/{id}", rutas.EnumerateObject().Select(ruta => ruta.Name));
        Assert.Contains("/api/v1/auditoria/entidades", rutas.EnumerateObject().Select(ruta => ruta.Name));
        Assert.Contains("/api/v1/auditoria/resumen", rutas.EnumerateObject().Select(ruta => ruta.Name));

        HttpResponseMessage inexistente = await _clienteHttp.GetAsync("/api/v1/auditoria/99999999");
        JsonDocument error = await JsonDocument.ParseAsync(await inexistente.Content.ReadAsStreamAsync());
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("El registro de auditoría 99999999 no existe.", error.RootElement.GetProperty("Message").GetString());
    }

    [Fact]
    public async Task ResumenIncluyeConteosPorEntidadYLosSieteDiasConDiasVacios()
    {
        await LimpiarAuditoriaAsync();
        DateTime hoy = DateTime.UtcNow.Date;
        await InsertarAuditoriaAsync(
            CrearRegistro("Cliente", "1", "Alta", "a", hoy, null, "[]"),
            CrearRegistro("Cliente", "2", "Modificacion", "b", hoy.AddDays(-2), null, "[]"),
            CrearRegistro("Producto", "3", "Baja", "c", hoy.AddDays(-6), null, "[]"));

        TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.Dtos.ResumenAuditoriaDto? resumen =
            await _clienteHttp.GetFromJsonAsync<TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.Dtos.ResumenAuditoriaDto>(
                "/api/v1/auditoria/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.Total);
        Assert.Equal(1, resumen.Altas);
        Assert.Equal(1, resumen.Modificaciones);
        Assert.Equal(1, resumen.Bajas);
        Assert.Equal(new[] { "Cliente", "Producto" }, resumen.PorEntidad.Select(item => item.Entidad).ToArray());
        Assert.Equal(7, resumen.Ultimos7Dias.Count);
        Assert.Equal(new[] { 1, 0, 0, 0, 1, 0, 1 }, resumen.Ultimos7Dias.Select(dia => dia.Registros).ToArray());
        Assert.Equal(hoy.AddDays(-6), resumen.Ultimos7Dias[0].Fecha);
        Assert.Equal(hoy, resumen.Ultimos7Dias[6].Fecha);
    }

    private async Task<HttpResponseMessage> EnviarClienteAsync(
        HttpMethod metodo,
        string ruta,
        GuardarClienteRequestDto solicitud,
        string usuario)
    {
        HttpRequestMessage request = new HttpRequestMessage(metodo, ruta)
        {
            Content = JsonContent.Create(solicitud)
        };
        request.Headers.Add("X-Usuario", usuario);
        return await _clienteHttp.SendAsync(request);
    }

    private async Task LimpiarAuditoriaAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.RegistrosAuditoria.RemoveRange(await contexto.RegistrosAuditoria.ToListAsync());
        await contexto.SaveChangesAsync();
    }

    private async Task<List<RegistroAuditoria>> ObtenerAuditoriaAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.RegistrosAuditoria.OrderBy(registro => registro.Id).ToListAsync();
    }

    private async Task<long> ContarAuditoriaAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.RegistrosAuditoria.LongCountAsync();
    }

    private async Task InsertarAuditoriaAsync(params RegistroAuditoria[] registros)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.RegistrosAuditoria.AddRange(registros);
        await contexto.SaveChangesAsync();
    }

    private static RegistroAuditoria CrearRegistro(
        string entidad,
        string entidadId,
        string accion,
        string usuario,
        DateTime fecha,
        string? ruta,
        string cambios)
    {
        return new RegistroAuditoria
        {
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Usuario = usuario,
            Fecha = DateTime.SpecifyKind(fecha, DateTimeKind.Utc),
            Ruta = ruta,
            Cambios = cambios
        };
    }

    private static List<CambioCampoDto> DeserializarCambios(string cambios)
    {
        return JsonSerializer.Deserialize<List<CambioCampoDto>>(
            cambios,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new List<CambioCampoDto>();
    }

    private static CambioCampoDto BuscarCampo(List<CambioCampoDto> cambios, string campo)
    {
        return Assert.Single(cambios, cambio => cambio.Campo == campo);
    }

    private static string CrearRucUnico()
    {
        return "20" + Random.Shared.Next(100000000, 1000000000).ToString(CultureInfo.InvariantCulture);
    }
}
