using System.Globalization;
using System.Text.Json;
using System.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Api.Configuration;

namespace TitanGlobal.Api.Models;

public partial class TitanGlobalDbContext
{
    private static readonly JsonSerializerOptions OpcionesJsonAuditoria = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public override int SaveChanges()
    {
        return GuardarConAuditoria(true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return GuardarConAuditoria(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return GuardarConAuditoriaAsync(true, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return GuardarConAuditoriaAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private int GuardarConAuditoria(bool acceptAllChangesOnSuccess)
    {
        ChangeTracker.DetectChanges();
        List<CambioPendienteAuditoria> cambiosPendientes = CapturarCambiosPendientes();
        if (cambiosPendientes.Count == 0)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        IDbContextTransaction? transaccion = IniciarTransaccionLocalSiCorresponde();
        try
        {
            int cantidad = base.SaveChanges(acceptAllChangesOnSuccess);
            AgregarRegistrosAuditoria(cambiosPendientes);
            base.SaveChanges(acceptAllChangesOnSuccess);
            transaccion?.Commit();
            return cantidad;
        }
        catch
        {
            transaccion?.Rollback();
            throw;
        }
        finally
        {
            transaccion?.Dispose();
        }
    }

    private async Task<int> GuardarConAuditoriaAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        List<CambioPendienteAuditoria> cambiosPendientes = CapturarCambiosPendientes();
        if (cambiosPendientes.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        IDbContextTransaction? transaccion = await IniciarTransaccionLocalAsyncSiCorresponde(cancellationToken);
        try
        {
            int cantidad = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            AgregarRegistrosAuditoria(cambiosPendientes);
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (transaccion != null)
            {
                await transaccion.CommitAsync(cancellationToken);
            }

            return cantidad;
        }
        catch
        {
            if (transaccion != null)
            {
                await transaccion.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaccion != null)
            {
                await transaccion.DisposeAsync();
            }
        }
    }

    private List<CambioPendienteAuditoria> CapturarCambiosPendientes()
    {
        List<CambioPendienteAuditoria> cambiosPendientes = new List<CambioPendienteAuditoria>();
        foreach (EntityEntry entrada in ChangeTracker.Entries())
        {
            if (entrada.Entity is RegistroAuditoria)
            {
                continue;
            }

            if (entrada.State != EntityState.Added && entrada.State != EntityState.Modified && entrada.State != EntityState.Deleted)
            {
                continue;
            }

            List<CambioCampoAuditoria> cambios = new List<CambioCampoAuditoria>();
            foreach (PropertyEntry propiedad in entrada.Properties)
            {
                if (entrada.State == EntityState.Modified && Equals(propiedad.OriginalValue, propiedad.CurrentValue))
                {
                    continue;
                }

                object? antes = entrada.State == EntityState.Added ? null : NormalizarValor(propiedad.OriginalValue);
                object? despues = entrada.State == EntityState.Deleted ? null : NormalizarValor(propiedad.CurrentValue);
                cambios.Add(new CambioCampoAuditoria(propiedad.Metadata.Name, antes, despues));
            }

            if (cambios.Count == 0)
            {
                continue;
            }

            string accion = string.Empty;
            if (entrada.State == EntityState.Added)
            {
                accion = "Alta";
            }
            else if (entrada.State == EntityState.Modified)
            {
                accion = "Modificacion";
            }
            else if (entrada.State == EntityState.Deleted)
            {
                accion = "Baja";
            }
            cambiosPendientes.Add(new CambioPendienteAuditoria(entrada, accion, cambios));
        }

        return cambiosPendientes;
    }

    private void AgregarRegistrosAuditoria(List<CambioPendienteAuditoria> cambiosPendientes)
    {
        foreach (CambioPendienteAuditoria cambioPendiente in cambiosPendientes)
        {
            string entidadId = ObtenerEntidadId(cambioPendiente.Entrada);
            string usuario = ObtenerUsuario();
            string? ruta = ObtenerRuta();
            List<CambioCampoAuditoria> cambios = cambioPendiente.Cambios;
            if (cambioPendiente.Accion == "Alta")
            {
                cambios = cambioPendiente.Cambios
                    .Select(cambio => new CambioCampoAuditoria(
                        cambio.Campo,
                        cambio.Antes,
                        NormalizarValor(cambioPendiente.Entrada.Property(cambio.Campo).CurrentValue)))
                    .ToList();
            }

            string cambiosJson = JsonSerializer.Serialize(cambios, OpcionesJsonAuditoria);
            RegistroAuditoria registro = new RegistroAuditoria
            {
                Fecha = DateTime.UtcNow,
                Entidad = cambioPendiente.Entrada.Metadata.ClrType.Name,
                EntidadId = entidadId,
                Accion = cambioPendiente.Accion,
                Usuario = usuario,
                Ruta = ruta,
                Cambios = cambiosJson
            };
            RegistrosAuditoria.Add(registro);
        }
    }

    private string ObtenerEntidadId(EntityEntry entrada)
    {
        IKey? clave = entrada.Metadata.FindPrimaryKey();
        if (clave == null)
        {
            return string.Empty;
        }

        List<string> valores = new List<string>();
        foreach (IProperty propiedadClave in clave.Properties)
        {
            object? valor = entrada.Property(propiedadClave.Name).CurrentValue;
            valores.Add(Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return string.Join("|", valores);
    }

    private string ObtenerUsuario()
    {
        HttpContext? contexto = _httpContextAccessor?.HttpContext;
        string? usuario = contexto?.Request.Headers["X-Usuario"].ToString();
        usuario = usuario?.Trim();
        if (string.IsNullOrEmpty(usuario))
        {
            return "sistema";
        }

        return usuario.Length > 30 ? usuario.Substring(0, 30) : usuario;
    }

    private string? ObtenerRuta()
    {
        HttpContext? contexto = _httpContextAccessor?.HttpContext;
        if (contexto == null)
        {
            return null;
        }

        string ruta = contexto.Request.Method + " " + contexto.Request.Path;
        return ruta.Length > 200 ? ruta.Substring(0, 200) : ruta;
    }

    private static object? NormalizarValor(object? valor)
    {
        if (valor == null)
        {
            return null;
        }

        if (valor is DateTime fecha)
        {
            return FechasUtc.Normalizar(fecha).ToString("O", CultureInfo.InvariantCulture);
        }

        if (valor is DateTimeOffset fechaConZona)
        {
            return fechaConZona.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        if (valor is decimal importe)
        {
            return importe.ToString(CultureInfo.InvariantCulture);
        }

        if (valor is IFormattable formateable)
        {
            return formateable.ToString(null, CultureInfo.InvariantCulture);
        }

        return valor.ToString();
    }

    private IDbContextTransaction? IniciarTransaccionLocalSiCorresponde()
    {
        if (!Database.IsRelational() || Transaction.Current != null || Database.CurrentTransaction != null)
        {
            return null;
        }

        return Database.BeginTransaction();
    }

    private async Task<IDbContextTransaction?> IniciarTransaccionLocalAsyncSiCorresponde(CancellationToken cancellationToken)
    {
        if (!Database.IsRelational() || Transaction.Current != null || Database.CurrentTransaction != null)
        {
            return null;
        }

        return await Database.BeginTransactionAsync(cancellationToken);
    }

    private void ConfigurarAuditoria(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroAuditoria>()
            .Property(registro => registro.Fecha)
            .HasColumnType("timestamp with time zone");
        modelBuilder.Entity<RegistroAuditoria>()
            .Property(registro => registro.Entidad)
            .HasMaxLength(100)
            .IsRequired();
        modelBuilder.Entity<RegistroAuditoria>()
            .Property(registro => registro.EntidadId)
            .HasMaxLength(50)
            .IsRequired();
        modelBuilder.Entity<RegistroAuditoria>()
            .Property(registro => registro.Accion)
            .HasMaxLength(20)
            .IsRequired();
        modelBuilder.Entity<RegistroAuditoria>()
            .Property(registro => registro.Usuario)
            .HasMaxLength(30)
            .IsRequired();
        modelBuilder.Entity<RegistroAuditoria>()
            .Property(registro => registro.Ruta)
            .HasMaxLength(200);
        modelBuilder.Entity<RegistroAuditoria>()
            .Property(registro => registro.Cambios)
            .IsRequired();
        modelBuilder.Entity<RegistroAuditoria>()
            .HasIndex(registro => registro.Fecha);
        modelBuilder.Entity<RegistroAuditoria>()
            .HasIndex(registro => new { registro.Entidad, registro.EntidadId });
    }

    private sealed record CambioPendienteAuditoria(EntityEntry Entrada, string Accion, List<CambioCampoAuditoria> Cambios);

    private sealed record CambioCampoAuditoria(string Campo, object? Antes, object? Despues);
}
