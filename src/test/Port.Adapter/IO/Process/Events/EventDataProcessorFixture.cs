﻿using Moq;
using org.neurul.Common.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using works.ei8.Cortex.Graph.Domain.Model;
using Xunit;

namespace works.ei8.Cortex.Graph.Port.Adapter.IO.Process.Events.Test.EventDataProcessorFixture.given
{
    public abstract class Context : TestContext<EventDataProcessor>
    {
        protected Mock<IRepository<Neuron>> repository;
        protected Guid guid;

        protected override void Given()
        {
            base.Given();

            this.repository = new Mock<IRepository<Neuron>>();
            this.sut = new EventDataProcessor();
            this.guid = Guid.NewGuid();
        }
    }

    public class When_processing_event
    {
        public abstract class ProcessContext : Context
        {
            protected Guid gettingGuid;
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
            }

            protected override void When()
            {
                Task.Run(() => this.sut.Process(this.repository.Object, this.EventName, this.Tag)).Wait();
            }

            protected abstract Neuron GetNeuron(Guid id);

            protected abstract string EventName { get; }

            protected abstract string Tag { get; }
        }

        public abstract class SavingContext : ProcessContext
        {
            protected Neuron savingNeuron;

            protected override void Given()
            {
                base.Given();

                this.repository
                    .Setup(e => e.Save(It.IsAny<Neuron>(), It.IsAny<CancellationToken>()))
                    .Callback<Neuron, CancellationToken>((n, c) => this.savingNeuron = n)
                    .Returns<Neuron, CancellationToken>((n, c) => Task.CompletedTask);
            }
        }

        public class When_neuron_created
        {
            public class NeuronCreatedContext : SavingContext
            {
                protected override Neuron GetNeuron(Guid id) => null;

                protected override string EventName => "NeuronCreated";

                protected override string Tag => $"{{\"Tag\":\"{this.initialTag}\",\"Id\":\"{this.guid.ToString()}\",\"Version\":{this.version},\"TimeStamp\":\"{this.timestamp}\"}}";
            }

            public class When_data_is_valid : NeuronCreatedContext
            {
                [Fact]
                public void Should_save_neuron()
                {
                    Assert.NotNull(this.savingNeuron);
                }

                [Fact]
                public void Should_save_tag()
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

                protected const string NewTag = "A whole new world";

                protected override string EventName => "NeuronTagChanged";

                protected override string Tag => $"{{\"Tag\":\"{TagChangedContext.NewTag}\",\"Id\":\"{this.guid.ToString()}\",\"Version\":{this.version},\"TimeStamp\":\"{this.timestamp}\"}}";
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

        public class When_terminal_added
        {
            public class TerminalAddedContext : SavingContext
            {
                protected string terminal1Id = Guid.NewGuid().ToString();
                protected string terminal2Id = Guid.NewGuid().ToString();

                protected override Neuron GetNeuron(Guid id)
                {
                    return new Neuron()
                    {
                        Id = id.ToString(),
                        Tag = this.initialTag
                    };
                }

                protected override string EventName => "TerminalsAdded";

                protected override string Tag => @"{
  ""Terminals"": [
    {
      ""TargetId"": """ + this.terminal1Id + @""",
      ""Effect"": 1,
      ""Strength"": 1.0 
    },
    {
      ""TargetId"": """ + this.terminal2Id + @""",
      ""Effect"": 1,
      ""Strength"": 1.0 
    }
  ],
  ""Id"": """ + this.guid.ToString() + @""",
  ""Version"": """ + this.version.ToString() + @""",
  ""TimeStamp"": """ + this.timestamp + @"""
}";
            }

            public class When_data_is_valid : TerminalAddedContext
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
                public void Should_save_two_terminals()
                {
                    Assert.Equal(2, this.savingNeuron.Terminals.Count());
                }

                [Fact]
                public void Should_save_correct_terminal1()
                {
                    Assert.Equal(this.terminal1Id, this.savingNeuron.Terminals.ToArray()[0].TargetId);
                }

                [Fact]
                public void Should_save_correct_terminal2()
                {
                    Assert.Equal(this.terminal2Id, this.savingNeuron.Terminals.ToArray()[1].TargetId);
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

        public class When_terminal_removed
        {
            public class TerminalRemovedContext : SavingContext
            {
                protected string terminal1Id = Guid.NewGuid().ToString();
                protected string terminal2Id = Guid.NewGuid().ToString();

                protected override Neuron GetNeuron(Guid id)
                {
                    return new Neuron()
                    {
                        Id = id.ToString(),
                        Tag = this.initialTag,
                        Terminals = new Terminal[]
                        {
                            new Terminal(Guid.NewGuid().ToString(), id.ToString(), this.terminal1Id, NeurotransmitterEffect.Excite, 1),
                            new Terminal(Guid.NewGuid().ToString(), id.ToString(), this.terminal2Id, NeurotransmitterEffect.Excite, 1),
                        }
                    };
                }

                protected override string EventName => "TerminalsRemoved";

                protected override string Tag => @"{
  ""TargetIds"": [
    """ + this.terminal1Id + @"""
  ],
  ""Id"": """ + this.guid.ToString() + @""",
  ""Version"": """ + this.version.ToString() + @""",
  ""TimeStamp"": """ + this.timestamp + @"""
}";
            }

            public class When_data_is_valid : TerminalRemovedContext
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
                public void Should_save_one_terminal_only()
                {
                    Assert.Single(this.savingNeuron.Terminals);
                }

                [Fact]
                public void Should_save_correct_terminal()
                {
                    Assert.Equal(this.terminal2Id, this.savingNeuron.Terminals.ToArray()[0].TargetId);
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

        public abstract class RemovalContext : ProcessContext
        {
            protected Neuron removingNeuron;

            protected override void Given()
            {
                base.Given();

                this.repository
                    .Setup(e => e.Remove(It.IsAny<Neuron>(), It.IsAny<CancellationToken>()))
                    .Callback<Neuron, CancellationToken>((n, c) => this.removingNeuron = n)
                    .Returns<Neuron, CancellationToken>((n, c) => Task.CompletedTask);
            }
        }

        public class When_neuron_deactivated
        {
            public class NeuronDeactivatedContext : RemovalContext
            {
                protected string terminalId = Guid.NewGuid().ToString();

                protected override Neuron GetNeuron(Guid id)
                {
                    return new Neuron()
                    {
                        Id = id.ToString(),
                        Tag = this.initialTag,
                        Terminals = new Terminal[]
                        {
                                new Terminal(Guid.NewGuid().ToString(), id.ToString(), this.terminalId, NeurotransmitterEffect.Excite, 1),
                        }
                    };
                }

                protected override string EventName => "NeuronDeactivated";

                protected override string Tag => @"{
  ""Id"": """ + this.guid.ToString() + @""",
  ""Version"": 4,
  ""TimeStamp"": ""2017-12-02T12:55:46.408498+00:00""
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
    }
}