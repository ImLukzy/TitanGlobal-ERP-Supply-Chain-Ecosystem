using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo17_AgentesAduaneros;

internal static class Modulo17Datos
{
    internal static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DeclaracionesAduana.RemoveRange(contexto.DeclaracionesAduana);
        contexto.AgentesAduaneros.RemoveRange(contexto.AgentesAduaneros);
        contexto.ExpedientesImportacion.RemoveRange(contexto.ExpedientesImportacion);
        contexto.DetallesOrdenesAbastecimiento.RemoveRange(contexto.DetallesOrdenesAbastecimiento);
        contexto.OrdenesAbastecimiento.RemoveRange(contexto.OrdenesAbastecimiento);
        contexto.Proveedores.RemoveRange(contexto.Proveedores);

        DateTime hoy = DateTime.UtcNow.Date;
        contexto.Proveedores.Add(new Proveedor { Id = 1, RazonSocial = "Proveedor Activo", Ruc = "20611111111", Pais = "Peru", Activo = true });
        contexto.OrdenesAbastecimiento.Add(new OrdenAbastecimiento
        {
            Id = 1,
            Numero = "OA-T-001",
            Fecha = hoy.AddDays(-10),
            ProveedorId = 1,
            Estado = "Emitida",
            FechaEntregaEstimada = hoy.AddDays(30)
        });
        contexto.ExpedientesImportacion.AddRange(
            CrearExpediente(1, "IMP-T-001", "EnTransito"),
            CrearExpediente(2, "IMP-T-002", "EnAduana"),
            CrearExpediente(3, "IMP-T-003", "Programado"),
            CrearExpediente(4, "IMP-T-004", "Liberado"));
        contexto.AgentesAduaneros.AddRange(
            CrearAgente(1, "20512345678", "Aduanas del Pacifico", "AA-0101", true),
            CrearAgente(2, "20523456789", "Despachos Andinos", "AA-0102", true),
            CrearAgente(3, "20534567890", "Agencia Norte", "AA-0103", false));
        contexto.DeclaracionesAduana.AddRange(
            CrearDeclaracion(1, "DAM-T-001", 1, 1, 50000m, 6m, "Registrada", null),
            CrearDeclaracion(2, "DAM-T-002", 2, 2, 20000m, 0m, "Numerada", "Naranja"),
            CrearDeclaracion(3, "DAM-T-003", 4, 1, 12000m, 11m, "Levantada", "Verde"),
            CrearDeclaracion(4, "DAM-T-004", 3, 1, 8000m, 4m, "Anulada", null));
        contexto.SaveChanges();
    }

    internal static async Task AssertBusinessErrorAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode codigoHttp,
        string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal(codigoHttp, respuesta.StatusCode);
        Assert.Equal((int)codigoHttp, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }

    internal static async Task<int> LeerIdAsync(HttpResponseMessage respuesta)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        return documento.RootElement.GetProperty("id").GetInt32();
    }

    private static ExpedienteImportacion CrearExpediente(int id, string numero, string estado)
    {
        DateOnly hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        return new ExpedienteImportacion
        {
            Id = id,
            Numero = numero,
            OrdenAbastecimientoId = 1,
            Incoterm = "FOB",
            MedioTransporte = "Maritimo",
            PaisOrigen = "Chile",
            LugarOrigen = "Valparaiso",
            LugarDestino = "Callao",
            FechaLlegadaEstimada = hoy.AddDays(20),
            Estado = estado,
            FechaRegistro = DateTime.UtcNow
        };
    }

    private static AgenteAduanero CrearAgente(int id, string ruc, string razonSocial, string registro, bool activo)
    {
        return new AgenteAduanero
        {
            Id = id,
            Ruc = ruc,
            RazonSocial = razonSocial,
            NumeroRegistro = registro,
            Activo = activo,
            FechaRegistro = DateTime.UtcNow
        };
    }

    private static DeclaracionAduana CrearDeclaracion(
        int id,
        string numero,
        int expedienteId,
        int agenteId,
        decimal valor,
        decimal tasa,
        string estado,
        string? canal)
    {
        return new DeclaracionAduana
        {
            Id = id,
            Numero = numero,
            ExpedienteImportacionId = expedienteId,
            AgenteAduaneroId = agenteId,
            Regimen = "ImportacionConsumo",
            ValorAduanero = valor,
            TasaArancelaria = tasa,
            DerechosArancel = decimal.Round(valor * tasa / 100m, 2),
            Canal = canal,
            Estado = estado,
            FechaNumeracion = canal is null ? null : DateTime.UtcNow,
            FechaLevante = estado == "Levantada" ? DateTime.UtcNow : null,
            MotivoAnulacion = estado == "Anulada" ? "Prueba" : null,
            FechaRegistro = DateTime.UtcNow.AddMinutes(id)
        };
    }
}
