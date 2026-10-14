namespace TitanGlobal.Api.Repositories.Interfaces;

public interface IGenericRepository<T> where T : class
{
    IQueryable<T> AsQueryable();

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);
}
