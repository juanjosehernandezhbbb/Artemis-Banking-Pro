using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Interfaces;
using ArtemisBankingPro.Persistance.Context;

namespace ArtemisBankingPro.Persistance.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);
        if (_repositories.ContainsKey(type))
            return (IGenericRepository<T>)_repositories[type]!;

        var repo = new GenericRepository<T>(_context);
        _repositories[type] = repo;
        return repo;
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
