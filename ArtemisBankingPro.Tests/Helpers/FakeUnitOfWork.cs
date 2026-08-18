using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Interfaces;

namespace ArtemisBankingPro.Tests.Helpers
{
    public class FakeUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = new();

        public int SaveChangesCalls { get; private set; }

        public FakeRepository<T> Repo<T>() where T : BaseEntity
            => (FakeRepository<T>)Repository<T>();

        public IGenericRepository<T> Repository<T>() where T : BaseEntity
        {
            var type = typeof(T);
            if (!_repositories.TryGetValue(type, out var repo))
            {
                repo = new FakeRepository<T>();
                _repositories[type] = repo;
            }
            return (IGenericRepository<T>)repo;
        }

        public Task<int> SaveChangesAsync()
        {
            SaveChangesCalls++;
            return Task.FromResult(0);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}