using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo61_Empleados.Dtos;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;

namespace TitanGlobal.Api.Tests.Modulo61_Empleados;

[Collection("Modulo04")]
public class EmpleadosApiTests
{
    private readonly OrdenesCompraApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public EmpleadosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    public static IEnumerable<object?[]> CasosDeCamposInvalidos()
    {
        List<(string Campo, string? Valor, int Estado, string Mensaje)> casos = new List<(string Campo, string? Valor, int Estado, string Mensaje)>
        {
            ("tipoDocumento", "Otro", 400, "El tipo de documento no es válido."),
            ("tipoDocumento", "dni", 400, "El tipo de documento no es válido."),
            ("numeroDocumento", "", 400, "El número de documento es obligatorio."),
            ("numeroDocumento", new string('1', 21), 400, "El número de documento no puede superar 20 caracteres."),
            ("numeroDocumento", "AB-123", 400, "El número de documento solo admite letras y números."),
            ("nombres", " ", 400, "Los nombres son obligatorios."),
            ("nombres", new string('N', 101), 400, "Los nombres no pueden superar 100 caracteres."),
            ("apellidos", "", 400, "Los apellidos son obligatorios."),
            ("apellidos", new string('A', 101), 400, "Los apellidos no pueden superar 100 caracteres."),
            ("fechaNacimiento", null, 400, "La fecha de nacimiento es obligatoria."),
            ("fechaIngreso", null, 400, "La fecha de ingreso es obligatoria."),
            ("cargo", "", 400, "El cargo es obligatorio."),
            ("cargo", new string('C', 101), 400, "El cargo no puede superar 100 caracteres."),
            ("area", " ", 400, "El área es obligatoria."),
            ("area", new string('A', 101), 400, "El área no puede superar 100 caracteres."),
            ("email", "no-valido", 400, "El email no tiene un formato válido."),
            ("email", new string('a', 140) + "@ejemplo.com", 400, "El email no tiene un formato válido."),
            ("telefono", new string('1', 21), 400, "El teléfono no puede superar 20 caracteres.")
        };
        List<object?[]> filas = new List<object?[]>();
        foreach (string metodo in new[] { "POST", "PUT" })
        {
            foreach ((string Campo, string? Valor, int Estado, string Mensaje) caso in casos)
            {
                filas.Add(new object?[] { metodo, caso.Campo, caso.Valor, caso.Estado, caso.Mensaje });
            }
        }

        return filas;
    }

    public static IEnumerable<object?[]> CasosDeCeseInvalido()
    {
        return new List<object?[]>
        {
            new object?[] { null, "Renuncia voluntaria", 400, "La fecha de cese es obligatoria." },
            new object?[] { "2026-10-31", " ", 400, "El motivo de cese es obligatorio." },
            new object?[] { "2026-10-31", new string('M', 301), 400, "El motivo de cese no puede superar 300 caracteres." },
            new object?[] { "2019-08-15", "Renuncia voluntaria", 400, "La fecha de cese no puede ser anterior a la fecha de ingreso." }
        };
    }

    [Theory]
    [MemberData(nameof(CasosDeCamposInvalidos))]
    public async Task CamposInvalidosDevuelvenMensajeLiteralSinCrearNiCambiarFilas(
        string metodo,
        string campo,
        string? valor,
        int estado,
        string mensaje)
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudConCampo(campo, valor);
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await EnviarGuardarAsync(metodo, 1, solicitud);
        await AssertErrorAsync(respuesta, estado, mensaje);
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task MenorDeDieciochoAlIngresoDevuelveLiteralSinCambiarFilas(string metodo)
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.FechaNacimiento = new DateOnly(2008, 3, 2);
        solicitud.FechaIngreso = new DateOnly(2026, 3, 1);
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await EnviarGuardarAsync(metodo, 1, solicitud);
        await AssertErrorAsync(respuesta, 400, "El empleado debe tener al menos 18 años a la fecha de ingreso.");
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task CumpleDieciochoEnLaFechaDeIngresoPermiteAlta()
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.FechaNacimiento = new DateOnly(2008, 3, 2);
        solicitud.FechaIngreso = new DateOnly(2026, 3, 2);
        int id = await CrearAltaValidaAsync(solicitud);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task DocumentoRepetidoEnAltaDevuelveConflictoSinCambiarFilas()
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.NumeroDocumento = "10000001";
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/empleados", solicitud);
        await AssertErrorAsync(respuesta, 409, "Ya existe un empleado con el documento DNI 10000001.");
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task MismoNumeroConOtroTipoDeDocumentoPermiteAlta()
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.TipoDocumento = "CE";
        solicitud.NumeroDocumento = "10000001";
        int id = await CrearAltaValidaAsync(solicitud);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task EditarConSuPropioDocumentoPermiteActualizar()
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.TipoDocumento = "DNI";
        solicitud.NumeroDocumento = "10000001";
        solicitud.Nombres = "Ana Maria";
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/empleados/1", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Empleado? empleado = await ObtenerEmpleadoAsync(1);
        Assert.NotNull(empleado);
        Assert.Equal("Ana Maria", empleado.Nombres);
        Assert.Equal("EMP-000001", empleado.Legajo);
    }

    [Fact]
    public async Task EditarConDocumentoAjenoDevuelveConflictoSinCambiarFilas()
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.TipoDocumento = "DNI";
        solicitud.NumeroDocumento = "10000002";
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/empleados/1", solicitud);
        await AssertErrorAsync(respuesta, 409, "Ya existe un empleado con el documento DNI 10000002.");
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task AltaAsignaLegajoIgualAlIdYEstadoActivo()
    {
        int id = await CrearAltaValidaAsync(CrearSolicitudValida());
        Empleado? empleado = await ObtenerEmpleadoAsync(id);
        Assert.NotNull(empleado);
        Assert.Equal("EMP-" + id.ToString("D6", CultureInfo.InvariantCulture), empleado.Legajo);
        Assert.Equal("Activo", empleado.Estado);
        EmpleadoDetalleDto? detalle = await _cliente.GetFromJsonAsync<EmpleadoDetalleDto>($"/api/v1/empleados/{id}");
        Assert.NotNull(detalle);
        Assert.Equal("EMP-" + id.ToString("D6", CultureInfo.InvariantCulture), detalle.Legajo);
        Assert.Equal("Activo", detalle.Estado);
    }

    [Theory]
    [InlineData("ANA", 1)]
    [InlineData("luis", 2)]
    [InlineData("ROJAS", 2)]
    [InlineData("10000002", 2)]
    [InlineData("p1234567", 4)]
    [InlineData("EMP-000003", 3)]
    public async Task BusquedaEncuentraPorNombresApellidosDocumentoYLegajoSinDistinguirMayusculas(string busqueda, int idEsperado)
    {
        PaginaEmpleadosDto? pagina = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>(
            "/api/v1/empleados?busqueda=" + Uri.EscapeDataString(busqueda));
        Assert.NotNull(pagina);
        Assert.Equal(1, pagina.Total);
        Assert.Equal(idEsperado, Assert.Single(pagina.Items).Id);
    }

    [Fact]
    public async Task FiltroPorEstadoDevuelveSoloEmpleadosDeEseEstado()
    {
        PaginaEmpleadosDto? cesados = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>("/api/v1/empleados?estado=Cesado");
        Assert.NotNull(cesados);
        Assert.Equal(1, cesados.Total);
        Assert.Equal(new[] { 4 }, cesados.Items.Select(item => item.Id).ToArray());
        PaginaEmpleadosDto? activos = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>("/api/v1/empleados?estado=Activo");
        Assert.NotNull(activos);
        Assert.Equal(3, activos.Total);
        Assert.Equal(new[] { 2, 3, 1 }, activos.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task ListadoOrdenaPorApellidosNombresYId()
    {
        PaginaEmpleadosDto? pagina = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>("/api/v1/empleados");
        Assert.NotNull(pagina);
        Assert.Equal(new[] { 4, 2, 3, 1 }, pagina.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task ListadoUsaTamanoPorDefectoDeDiezYDevuelveTotal()
    {
        PaginaEmpleadosDto? pagina = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>("/api/v1/empleados");
        Assert.NotNull(pagina);
        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(10, pagina.TamanoPagina);
        Assert.Equal(4, pagina.Total);
        Assert.Equal(4, pagina.Items.Count);
    }

    [Fact]
    public async Task ListadoPaginaConTotalGlobal()
    {
        PaginaEmpleadosDto? pagina = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>("/api/v1/empleados?pagina=2&tamanoPagina=3");
        Assert.NotNull(pagina);
        Assert.Equal(2, pagina.Pagina);
        Assert.Equal(3, pagina.TamanoPagina);
        Assert.Equal(4, pagina.Total);
        Assert.Equal(new[] { 1 }, pagina.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task TamanoPaginaCienEsValido()
    {
        PaginaEmpleadosDto? pagina = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>("/api/v1/empleados?tamanoPagina=100");
        Assert.NotNull(pagina);
        Assert.Equal(100, pagina.TamanoPagina);
    }

    [Fact]
    public async Task EstadoVacioSeTrataComoOmitido()
    {
        PaginaEmpleadosDto? pagina = await _cliente.GetFromJsonAsync<PaginaEmpleadosDto>("/api/v1/empleados?estado=");
        Assert.NotNull(pagina);
        Assert.Equal(4, pagina.Total);
    }

    [Theory]
    [InlineData("pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("estado=Otro", "El estado debe ser Activo o Cesado.")]
    [InlineData("estado=activo", "El estado debe ser Activo o Cesado.")]
    public async Task ParametrosInvalidosDelListadoDevuelvenLiteral(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/empleados?" + consulta);
        await AssertErrorAsync(respuesta, 400, mensaje);
    }

    [Fact]
    public async Task IdInexistenteDevuelveLiteralEnDetalleYNoCambiaFilas()
    {
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/empleados/9999");
        await AssertErrorAsync(respuesta, 404, "El empleado 9999 no existe.");
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task DetalleDevuelveFechasEnFormatoIsoIdaYVuelta()
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.FechaNacimiento = new DateOnly(1992, 12, 31);
        solicitud.FechaIngreso = new DateOnly(2024, 2, 29);
        int id = await CrearAltaValidaAsync(solicitud);
        HttpResponseMessage respuesta = await _cliente.GetAsync($"/api/v1/empleados/{id}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("1992-12-31", documento.RootElement.GetProperty("fechaNacimiento").GetString());
        Assert.Equal("2024-02-29", documento.RootElement.GetProperty("fechaIngreso").GetString());
        EmpleadoDetalleDto? detalle = await _cliente.GetFromJsonAsync<EmpleadoDetalleDto>($"/api/v1/empleados/{id}");
        Assert.NotNull(detalle);
        Assert.Equal(new DateOnly(1992, 12, 31), detalle.FechaNacimiento);
    }

    [Fact]
    public async Task DetalleDevuelveNulosLosCamposOpcionalesYDatosDeCese()
    {
        EmpleadoDetalleDto? detalle = await _cliente.GetFromJsonAsync<EmpleadoDetalleDto>("/api/v1/empleados/4");
        Assert.NotNull(detalle);
        Assert.Null(detalle.Email);
        Assert.Null(detalle.Telefono);
        Assert.Equal(new DateOnly(2026, 9, 30), detalle.FechaCese);
        Assert.Equal("Renuncia voluntaria", detalle.MotivoCese);
    }

    [Fact]
    public async Task CeseValidoCambiaEstadoFechaYMotivo()
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/empleados/2/cese",
            CrearCese(new DateOnly(2026, 10, 31), "Renuncia voluntaria"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        EmpleadoIdDto? resultado = await respuesta.Content.ReadFromJsonAsync<EmpleadoIdDto>();
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Id);
        Empleado? empleado = await ObtenerEmpleadoAsync(2);
        Assert.NotNull(empleado);
        Assert.Equal("Cesado", empleado.Estado);
        Assert.Equal(new DateOnly(2026, 10, 31), empleado.FechaCese);
        Assert.Equal("Renuncia voluntaria", empleado.MotivoCese);
        Assert.Equal("EMP-000002", empleado.Legajo);
    }

    [Fact]
    public async Task CeseEnLaFechaDeIngresoEsValido()
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/empleados/3/cese",
            CrearCese(new DateOnly(2024, 1, 8), "Renuncia voluntaria"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Empleado? empleado = await ObtenerEmpleadoAsync(3);
        Assert.NotNull(empleado);
        Assert.Equal("Cesado", empleado.Estado);
        Assert.Equal(new DateOnly(2024, 1, 8), empleado.FechaCese);
    }

    [Theory]
    [MemberData(nameof(CasosDeCeseInvalido))]
    public async Task CeseInvalidoDevuelveMensajeLiteralSinCambiarFilas(
        string? fechaCese,
        string? motivo,
        int estado,
        string mensaje)
    {
        CeseEmpleadoRequestDto solicitud = new CeseEmpleadoRequestDto
        {
            FechaCese = ConvertirFecha(fechaCese),
            Motivo = motivo
        };
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/empleados/2/cese", solicitud);
        await AssertErrorAsync(respuesta, estado, mensaje);
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task CeseRepetidoDevuelveConflictoSinCambiarFilas()
    {
        HttpResponseMessage primera = await _cliente.PostAsJsonAsync(
            "/api/v1/empleados/2/cese",
            CrearCese(new DateOnly(2026, 10, 31), "Renuncia voluntaria"));
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage segunda = await _cliente.PostAsJsonAsync(
            "/api/v1/empleados/2/cese",
            CrearCese(new DateOnly(2026, 10, 30), "Otro motivo"));
        await AssertErrorAsync(segunda, 409, "El empleado 2 ya está cesado.");
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task CeseDeEmpleadoInexistenteDevuelveLiteral()
    {
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/empleados/9999/cese",
            CrearCese(new DateOnly(2026, 10, 31), "Renuncia voluntaria"));
        await AssertErrorAsync(respuesta, 404, "El empleado 9999 no existe.");
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task EditarEmpleadoCesadoDevuelveConflictoSinCambiarFilas()
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        solicitud.TipoDocumento = "Pasaporte";
        solicitud.NumeroDocumento = "P1234567";
        solicitud.Nombres = "Carlos Alberto";
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync("/api/v1/empleados/4", solicitud);
        await AssertErrorAsync(respuesta, 409, "No se puede editar un empleado cesado.");
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task DeleteNoEliminaElEmpleado()
    {
        string[] antes = await LeerEmpleadosAsync();
        HttpResponseMessage respuesta = await _cliente.DeleteAsync("/api/v1/empleados/1");
        Assert.False(respuesta.IsSuccessStatusCode);
        Assert.NotNull(await ObtenerEmpleadoAsync(1));
        Assert.Equal(antes, await LeerEmpleadosAsync());
    }

    [Fact]
    public async Task AltaYCeseGeneranRegistrosDeAuditoriaDelEmpleado()
    {
        int id = await CrearAltaValidaAsync(CrearSolicitudValida());
        List<RegistroAuditoria> auditoriaAlta = await LeerAuditoriaEmpleadoAsync(id);
        Assert.Single(auditoriaAlta, registro => registro.Accion == "Alta");
        int modificacionesAntesDelCese = auditoriaAlta.Count(registro => registro.Accion == "Modificacion");

        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/empleados/{id}/cese",
            CrearCese(new DateOnly(2026, 10, 31), "Renuncia voluntaria"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        List<RegistroAuditoria> auditoriaCese = await LeerAuditoriaEmpleadoAsync(id);
        Assert.All(auditoriaCese, registro => Assert.Equal("Empleado", registro.Entidad));
        Assert.True(auditoriaCese.Count(registro => registro.Accion == "Modificacion") > modificacionesAntesDelCese);
    }

    [Fact]
    public async Task OpenApiIncluyeOperacionesDeEmpleados()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");
        Assert.True(rutas.GetProperty("/api/v1/empleados").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/empleados").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/empleados/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/empleados/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/empleados/{id}/cese").TryGetProperty("post", out _));
        Assert.False(rutas.GetProperty("/api/v1/empleados/{id}").TryGetProperty("delete", out _));
    }

    private async Task<int> CrearAltaValidaAsync(GuardarEmpleadoRequestDto solicitud)
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/empleados", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        EmpleadoIdDto? resultado = await respuesta.Content.ReadFromJsonAsync<EmpleadoIdDto>();
        Assert.NotNull(resultado);
        return resultado.Id;
    }

    private async Task<HttpResponseMessage> EnviarGuardarAsync(string metodo, int id, GuardarEmpleadoRequestDto solicitud)
    {
        if (metodo == "POST")
        {
            return await _cliente.PostAsJsonAsync("/api/v1/empleados", solicitud);
        }

        return await _cliente.PutAsJsonAsync($"/api/v1/empleados/{id}", solicitud);
    }

    private async Task<string[]> LeerEmpleadosAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        List<Empleado> empleados = await contexto.Empleados.AsNoTracking().OrderBy(empleado => empleado.Id).ToListAsync();
        return empleados.Select(FirmaEmpleado).ToArray();
    }

    private async Task<Empleado?> ObtenerEmpleadoAsync(int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Empleados.AsNoTracking().SingleOrDefaultAsync(empleado => empleado.Id == id);
    }

    private async Task<List<RegistroAuditoria>> LeerAuditoriaEmpleadoAsync(int id)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        string idTexto = id.ToString(CultureInfo.InvariantCulture);
        return await contexto.RegistrosAuditoria.AsNoTracking()
            .Where(registro => registro.Entidad == "Empleado" && registro.EntidadId == idTexto)
            .OrderBy(registro => registro.Id)
            .ToListAsync();
    }

    private void RestablecerDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        BorrarAuditoriaDeEmpleados(contexto);
        contexto.Empleados.AddRange(
            new Empleado
            {
                Id = 1,
                Legajo = "EMP-000001",
                TipoDocumento = "DNI",
                NumeroDocumento = "10000001",
                Nombres = "Ana",
                Apellidos = "Torres Vega",
                FechaNacimiento = new DateOnly(1990, 5, 14),
                FechaIngreso = new DateOnly(2021, 3, 1),
                Cargo = "Gerente de Ventas",
                Area = "Ventas",
                Email = "ana.torres@titanglobal.example",
                Telefono = "999000001",
                Estado = "Activo"
            },
            new Empleado
            {
                Id = 2,
                Legajo = "EMP-000002",
                TipoDocumento = "DNI",
                NumeroDocumento = "10000002",
                Nombres = "Luis",
                Apellidos = "Rojas Mendoza",
                FechaNacimiento = new DateOnly(1988, 11, 2),
                FechaIngreso = new DateOnly(2019, 8, 16),
                Cargo = "Analista de Compras",
                Area = "Compras",
                Email = "luis.rojas@titanglobal.example",
                Telefono = "999000002",
                Estado = "Activo"
            },
            new Empleado
            {
                Id = 3,
                Legajo = "EMP-000003",
                TipoDocumento = "CE",
                NumeroDocumento = "004512367",
                Nombres = "Maria",
                Apellidos = "Salazar Quispe",
                FechaNacimiento = new DateOnly(1995, 2, 20),
                FechaIngreso = new DateOnly(2024, 1, 8),
                Cargo = "Almacenero",
                Area = "Almacen",
                Email = "maria.salazar@titanglobal.example",
                Telefono = "999000003",
                Estado = "Activo"
            },
            new Empleado
            {
                Id = 4,
                Legajo = "EMP-000004",
                TipoDocumento = "Pasaporte",
                NumeroDocumento = "P1234567",
                Nombres = "Carlos",
                Apellidos = "Diaz Soto",
                FechaNacimiento = new DateOnly(1982, 7, 30),
                FechaIngreso = new DateOnly(2018, 5, 2),
                Cargo = "Auxiliar de Logistica",
                Area = "Logistica",
                Estado = "Cesado",
                FechaCese = new DateOnly(2026, 9, 30),
                MotivoCese = "Renuncia voluntaria"
            });
        contexto.SaveChanges();
        BorrarAuditoriaDeEmpleados(contexto);
    }

    private static void BorrarAuditoriaDeEmpleados(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria.Where(registro => registro.Entidad == "Empleado").ToList());
        contexto.SaveChanges();
    }

    private static string FirmaEmpleado(Empleado empleado)
    {
        return $"{empleado.Id}|{empleado.Legajo}|{empleado.TipoDocumento}|{empleado.NumeroDocumento}|{empleado.Nombres}|{empleado.Apellidos}|{empleado.FechaNacimiento:yyyy-MM-dd}|{empleado.FechaIngreso:yyyy-MM-dd}|{empleado.Cargo}|{empleado.Area}|{empleado.Email}|{empleado.Telefono}|{empleado.Estado}|{empleado.FechaCese:yyyy-MM-dd}|{empleado.MotivoCese}";
    }

    private static GuardarEmpleadoRequestDto CrearSolicitudValida()
    {
        return new GuardarEmpleadoRequestDto
        {
            TipoDocumento = "DNI",
            NumeroDocumento = "30000001",
            Nombres = "Lucia",
            Apellidos = "Quispe Ramos",
            FechaNacimiento = new DateOnly(1990, 1, 1),
            FechaIngreso = new DateOnly(2026, 3, 1),
            Cargo = "Analista de Compras",
            Area = "Compras",
            Email = null,
            Telefono = null
        };
    }

    private static GuardarEmpleadoRequestDto CrearSolicitudConCampo(string campo, string? valor)
    {
        GuardarEmpleadoRequestDto solicitud = CrearSolicitudValida();
        switch (campo)
        {
            case "tipoDocumento":
                solicitud.TipoDocumento = valor;
                break;
            case "numeroDocumento":
                solicitud.NumeroDocumento = valor;
                break;
            case "nombres":
                solicitud.Nombres = valor;
                break;
            case "apellidos":
                solicitud.Apellidos = valor;
                break;
            case "fechaNacimiento":
                solicitud.FechaNacimiento = null;
                break;
            case "fechaIngreso":
                solicitud.FechaIngreso = null;
                break;
            case "cargo":
                solicitud.Cargo = valor;
                break;
            case "area":
                solicitud.Area = valor;
                break;
            case "email":
                solicitud.Email = valor;
                break;
            case "telefono":
                solicitud.Telefono = valor;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(campo), campo, "Campo de prueba desconocido.");
        }

        return solicitud;
    }

    private static CeseEmpleadoRequestDto CrearCese(DateOnly fechaCese, string motivo)
    {
        return new CeseEmpleadoRequestDto
        {
            FechaCese = fechaCese,
            Motivo = motivo
        };
    }

    private static DateOnly? ConvertirFecha(string? valor)
    {
        DateOnly? fecha = null;
        if (valor is not null)
        {
            fecha = DateOnly.Parse(valor, CultureInfo.InvariantCulture);
        }

        return fecha;
    }

    private static async Task AssertErrorAsync(HttpResponseMessage respuesta, int estado, string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());
        Assert.Equal((HttpStatusCode)estado, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
