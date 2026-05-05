public interface IGenericRepository<T> where T : class
{
    Task<T?> GetById(int id);

    Task<List<T>> GetAll();

    Task<T> Add(T entity);

    void Update(T entity);

    void Delete(T entity);
}