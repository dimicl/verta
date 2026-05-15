public interface IGenericRepository<T> where T : class
{
    Task<T?> GetById(int id);

    Task<List<T>> GetAll();

    Task<T> Add(T entity);

    Task Update(T entity);

    Task Delete(T entity);
}