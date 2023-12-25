using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ei8.Cortex.Graph.Application
{
    public interface IPersistenceService
    {
        Task InitializeAsync();
    }
}
