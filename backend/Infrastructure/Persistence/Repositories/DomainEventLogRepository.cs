using backend.Infrastructure.Persistence;

public class DomainEventLogRepository : GenericRepository<DomainEventLog>, IDomainEventLogRepository
{
    public DomainEventLogRepository(AppDbContext context) : base(context) { }
}