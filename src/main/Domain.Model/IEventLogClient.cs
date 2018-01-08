using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace works.ei8.Brain.Graph.Domain.Model
{
    public interface IEventLogClient
    {
        Task Subscribe(); 

        Task Subscribe(string position); 
    }
}
