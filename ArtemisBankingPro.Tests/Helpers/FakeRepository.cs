using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Interfaces;

namespace ArtemisBankingPro.Tests.Helpers
{
    public class FakeRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly List<T> _items;
        private int _nextId = 1;

        public FakeRepository(List<T>? seed = null)
        {
            _items = seed ?? new List<T>();
            if (_items.Count > 0)
                _nextId = _items.Max(x => x.Id) + 1;
        }

        public List<T> Items => _items;

        public Task<T?> GetByIdAsync(int id)
            => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<T>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<T>>(_items.ToList());

        public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
            => Task.FromResult<IReadOnlyList<T>>(_items.AsQueryable().Where(predicate).ToList());

        public Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, Expression<Func<T, bool>>? predicate = null)
        {
            var query = predicate == null ? _items.AsQueryable() : _items.AsQueryable().Where(predicate);
            var total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(((IReadOnlyList<T>)items, total));
        }

        public Task AddAsync(T entity)
        {
            if (entity.Id == 0)
                entity.Id = _nextId++;
            else
                _nextId = Math.Max(_nextId, entity.Id + 1);
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
        }

        public void Remove(T entity) => _items.Remove(entity);
    }
}