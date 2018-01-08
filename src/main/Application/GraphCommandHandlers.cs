using CQRSlite.Commands;
using works.ei8.Brain.Graph.Application.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Brain.Graph.Domain.Model;

namespace works.ei8.Brain.Graph.Application
{
    public class GraphCommandHandlers : 
        ICancellableCommandHandler<Regenerate>,
        ICancellableCommandHandler<ResumeGeneration>
    {
        private IEventLogClient eventLog;
        private IRepository<NeuronVertex> neuronRepository;
        private IRepository<Settings> settingsRepository;

        public GraphCommandHandlers(IEventLogClient eventLog, IRepository<NeuronVertex> neuronRepository, 
            IRepository<Settings> settingsRepository)
        {
            this.eventLog = eventLog;
            this.neuronRepository = neuronRepository;
            this.settingsRepository = settingsRepository;
        }

        public Task Handle(Regenerate message, CancellationToken token = default(CancellationToken))
        {
            this.RegenerateCore();

            return Task.CompletedTask;
        }

        public Task Handle(ResumeGeneration message, CancellationToken token = default(CancellationToken))
        {
            var s = this.settingsRepository.Get(Guid.Empty)?.Result;

            if (s == null)
                this.RegenerateCore();
            else
                this.eventLog.Subscribe(s.LastPosition);

            return Task.CompletedTask;
        }

        private void RegenerateCore()
        {
            this.neuronRepository.Clear();
            this.settingsRepository.Clear();

            this.eventLog.Subscribe();
        }
    }
}
