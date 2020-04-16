using System;
using System.Threading;
using System.Threading.Tasks;

namespace works.ei8.Cortex.Graph.Domain.Model
{
    public interface IRepository<T> where T : class
    {
        Task<T> Get(Guid guid, CancellationToken cancellationToken = default(CancellationToken));

        Task Save(T value, CancellationToken cancellationToken = default(CancellationToken));

        Task Remove(T value, CancellationToken cancellationToken = default(CancellationToken));

        Task Clear();

        Task Initialize();
    }
}
