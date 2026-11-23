using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class UnitOfWork : IUnitOfWorkFacturas
{
    private readonly TitanGlobalDbContext _context;

    private readonly Dictionary<Type, object> _repositories = new Dictionary<Type, object>();

    public UnitOfWork(TitanGlobalDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        Type type = typeof(T);

        if (!_repositories.TryGetValue(type, out object? repository))
        {
            repository = new GenericRepository<T>(_context);
            _repositories.Add(type, repository);
        }

        return (IGenericRepository<T>)repository;
    }

    public async Task<int> CompleteAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateException excepcion) when (excepcion.InnerException is PostgresException postgres
            && EsViolacionConocida(postgres))
        {
            throw new ViolacionRestriccionException(
                ObtenerTipo(postgres),
                postgres.ConstraintName,
                excepcion.Message,
                excepcion);
        }
    }

    public async Task<TResult> ConFacturaBloqueadaAsync<TResult>(int facturaId, Func<Task<TResult>> operacion)
    {
        if (!_context.Database.IsRelational())
        {
            return await operacion();
        }

        await using IDbContextTransaction transaccion =
            await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT \"Id\" FROM \"Facturas\" WHERE \"Id\" = {facturaId} FOR UPDATE");

        TResult resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }

    private static bool EsViolacionConocida(PostgresException excepcion)
    {
        return excepcion.SqlState == PostgresErrorCodes.UniqueViolation
            || excepcion.SqlState == PostgresErrorCodes.ForeignKeyViolation;
    }

    private static TipoViolacionRestriccion ObtenerTipo(PostgresException excepcion)
    {
        if (excepcion.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return TipoViolacionRestriccion.Unicidad;
        }

        return TipoViolacionRestriccion.ReferenciaForanea;
    }

    // DbContext lifetime belongs to the dependency injection container.
    public void Dispose()
    {
    }
}
