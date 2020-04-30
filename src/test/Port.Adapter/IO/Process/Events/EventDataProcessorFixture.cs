using Moq;
using neurUL.Common.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Domain.Model;
using Xunit;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Test.EventDataProcessorFixture.given
{
    public abstract class Context : TestContext<EventDataProcessor>
    {
        protected Mock<IRepository<Neuron>> repository;
        protected Mock<IRepository<Terminal>> terminalRepository;
        protected Guid guid;

        protected override void Given()
        {
            base.Given();

            this.repository = new Mock<IRepository<Neuron>>();
            this.terminalRepository = new Mock<IRepository<Terminal>>();
            this.sut = new EventDataProcessor();
            this.guid = Guid.NewGuid();
        }
    }

    public class When_processing_event
    {
        public abstract class ProcessContext : Context
        {
            protected Guid gettingGuid;
            protected Guid gettingTerminalGuid;
            protected string initialTag;
            protected int version;
            protected string timestamp;

            protected override void Given()
            {
                base.Given();

                this.initialTag = "Hello World";
                this.version = new Random().Next();
                this.timestamp = DateTimeOffset.Now.ToString("o");

                this.repository
                    .Setup(e => e.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Callback<Guid, CancellationToken>((g, c) => this.gettingGuid = g)
                    .Returns<Guid, CancellationToken>((g, c) => Task.FromResult(this.GetNeuron(g)));

                this.terminalRepository
                    .Setup(e => e.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Callback<Guid, CancellationToken>((g, c) => this.gettingTerminalGuid = g)
                    .Returns<Guid, CancellationToken>((g, c) => Task.FromResult(this.GetTerminal(g)));
            }

            protected override void When()
            {
                // TODO: Task.Run(() => this.sut.Process(this.repository.Object, this.terminalRepository.Object, this.EventName, this.Data)).Wait();
            }

            protected abstract Neuron GetNeuron(Guid id);

            protected abstract Terminal GetTerminal(Guid id);

            protected abstract string EventName { get; }

            protected abstract string Data { get; }
        }

        public abstract class SavingContext : ProcessContext
        {
            protected Neuron savingNeuron;

            protected Terminal savingTerminal;

            protected override void Given()
            {
                base.Given();

                this.repository
                    .Setup(e => e.Save(It.IsAny<Neuron>(), It.IsAny<CancellationToken>()))
                    .Callback<Neuron, CancellationToken>((n, c) => this.savingNeuron = n)
                    .Returns<Neuron, CancellationToken>((n, c) => Task.CompletedTask);

                this.terminalRepository
                    .Setup(e => e.Save(It.IsAny<Terminal>(), It.IsAny<CancellationToken>()))
                    .Callback<Terminal, CancellationToken>((n, c) => this.savingTerminal = n)
                    .Returns<Terminal, CancellationToken>((n, c) => Task.CompletedTask);
            }
        }

        public class When_neuron_created
        {
            public class NeuronCreatedContext : SavingContext
            {
                protected override Neuron GetNeuron(Guid id) => null;

                protected override Terminal GetTerminal(Guid id) => null;

                protected override string EventName => "NeuronCreated";

                protected override string Data => $"{{\"Tag\":\"{this.initialTag}\",\"Id\":\"{this.guid.ToString()}\",\"Version\":{this.version},\"Timestamp\":\"{this.timestamp}\"}}";
            }

            public class When_data_is_valid : NeuronCreatedContext
            {
                [Fact]
                public void Should_save_neuron()
                {
                    Assert.NotNull(this.savingNeuron);
                }

                [Fact]
                public void Should_save_data()
                {
                    Assert.StartsWith(this.initialTag, this.savingNeuron.Tag);
                }

                [Fact]
                public void Should_save_correct_version()
                {
                    Assert.Equal(this.version, this.savingNeuron.Version);
                }

                [Fact]
                public void Should_save_correct_timestamp()
                {
                    Assert.Equal(this.timestamp, this.savingNeuron.Timestamp);
                }
            }
        }

        public class When_tag_changed
        {
            public class TagChangedContext : SavingContext
            {
                protected override Neuron GetNeuron(Guid id)
                {
                    return new Neuron() { Id = id.ToString(), Tag = this.initialTag };
                }

                protected override Terminal GetTerminal(Guid id) => null;

                protected const string NewTag = "A whole new world";

                protected override string EventName => "NeuronTagChanged";

                protected override string Data => $"{{\"Tag\":\"{TagChangedContext.NewTag}\",\"Id\":\"{this.guid.ToString()}\",\"Version\":{this.version},\"Timestamp\":\"{this.timestamp}\"}}";
            }

            public class When_data_is_valid : TagChangedContext
            {
                [Fact]
                public void Should_get_correct_guid()
                {
                    Assert.Equal(this.guid, this.gettingGuid);
                }

                [Fact]
                public void Should_save_neuron()
                {
                    Assert.NotNull(this.savingNeuron);
                }

                [Fact]
                public void Should_save_new_tag()
                {
                    Assert.StartsWith(TagChangedContext.NewTag, this.savingNeuron.Tag);
                }

                [Fact]
                public void Should_save_correct_version()
                {
                    Assert.Equal(this.version, this.savingNeuron.Version);
                }

                [Fact]
                public void Should_save_correct_timestamp()
                {
                    Assert.Equal(this.timestamp, this.savingNeuron.Timestamp);
                }
            }
        }

        public class When_terminal_created
        {
            public class TerminalCreatedContext : SavingContext
            {
                protected string terminal1Id = Guid.NewGuid().ToString();
                protected string target1Id = Guid.NewGuid().ToString();
                protected override Terminal GetTerminal(Guid id) => null;
                protected override Neuron GetNeuron(Guid id) => null;

                protected override string EventName => "TerminalCreated";

                protected override string Data => @"{
  ""PresynapticNeuronId"":""" + this.guid.ToString() + @""",
  ""PostsynapticNeuronId"":""" + this.target1Id.ToString() + @""",
  ""Effect"": 1,
  ""Strength"": 1.0,
  ""Id"": """ + this.terminal1Id.ToString() + @""",
  ""Version"": """ + this.version.ToString() + @""",
  ""Timestamp"": """ + this.timestamp + @"""
}";
            }

            public class When_data_is_valid : TerminalCreatedContext
            {
                [Fact]
                public void Should_save_terminal()
                {
                    Assert.NotNull(this.savingTerminal);
                }

                [Fact]
                public void Should_save_correct_terminal1()
                {
                    Assert.Equal(this.terminal1Id, this.savingTerminal.Id);
                }

                [Fact]
                public void Should_save_correct_pretsynapticNeuronId1()
                {
                    Assert.Equal(this.guid.ToString(), this.savingTerminal.PresynapticNeuronId);
                }

                [Fact]
                public void Should_save_correct_postsynapticNeuronId1()
                {
                    Assert.Equal(this.target1Id, this.savingTerminal.PostsynapticNeuronId);
                }

                [Fact]
                public void Should_save_correct_version()
                {
                    Assert.Equal(this.version, this.savingTerminal.Version);
                }

                [Fact]
                public void Should_save_correct_timestamp()
                {
                    Assert.Equal(this.timestamp, this.savingTerminal.Timestamp);
                }
            }            
        }

        public abstract class RemovalContext : ProcessContext
        {
            protected Neuron removingNeuron;
            protected Terminal removingTerminal;

            protected override void Given()
            {
                base.Given();

                this.repository
                    .Setup(e => e.Remove(It.IsAny<Neuron>(), It.IsAny<CancellationToken>()))
                    .Callback<Neuron, CancellationToken>((n, c) => this.removingNeuron = n)
                    .Returns<Neuron, CancellationToken>((n, c) => Task.CompletedTask);

                this.terminalRepository
                    .Setup(e => e.Remove(It.IsAny<Terminal>(), It.IsAny<CancellationToken>()))
                    .Callback<Terminal, CancellationToken>((n, c) => this.removingTerminal = n)
                    .Returns<Terminal, CancellationToken>((n, c) => Task.CompletedTask);
            }
        }

        public class When_neuron_deactivated
        {
            public class NeuronDeactivatedContext : RemovalContext
            {
                protected override Neuron GetNeuron(Guid id)
                {
                    return new Neuron()
                    {
                        Id = id.ToString(),
                        Tag = this.initialTag
                    };
                }

                protected override Terminal GetTerminal(Guid id) => null;

                protected override string EventName => "NeuronDeactivated";

                protected override string Data => @"{
  ""Id"": """ + this.guid.ToString() + @""",
  ""Version"": 4,
  ""Timestamp"": ""2017-12-02T12:55:46.408498+00:00""
}";
            }

            public class When_data_is_valid : NeuronDeactivatedContext
            {
                [Fact]
                public void Should_get_correct_guid()
                {
                    Assert.Equal(this.guid, this.gettingGuid);
                }

                [Fact]
                public void Should_save_neuron()
                {
                    Assert.NotNull(this.removingNeuron);
                }
            }
        }

        public class When_terminal_deactivated
        {
            public class DeactivatedContext : RemovalContext
            {
                protected override Neuron GetNeuron(Guid id) => null;

                protected override Terminal GetTerminal(Guid id) =>
                    new Terminal(
                        id.ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        NeurotransmitterEffect.Excite,
                        1f
                        );

            protected override string EventName => "TerminalDeactivated";

                protected override string Data => @"{
  ""Id"": """ + this.guid.ToString() + @""",
  ""Version"": 2,
  ""Timestamp"": ""2017-12-02T12:55:46.408498+00:00""
}";
            }

            public class When_data_is_valid : DeactivatedContext
            {
                [Fact]
                public void Should_get_correct_guid()
                {
                    Assert.Equal(this.guid, this.gettingTerminalGuid);
                }

                [Fact]
                public void Should_save_neuron()
                {
                    Assert.NotNull(this.removingTerminal);
                }
            }
        }
    }
}