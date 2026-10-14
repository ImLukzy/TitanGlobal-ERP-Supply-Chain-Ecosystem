using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
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
        return await _context.SaveChangesAsync();
    }

    // DbContext lifetime belongs to the dependency injection container.
    public void Dispose()
    {
    }
}
