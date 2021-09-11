using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ei8.Cortex.Graph.Application
{
    public static class ExtensionMethods
    {
        public static Common.QueryResult ToCommon(this Domain.Model.QueryResult value, string centralId = null)
        {
            Common.QueryResult result = new Common.QueryResult();

            result.Count = value.Count;
            result.Neurons = value.Neurons.Select(n => n.ToCommon(centralId));

            return result;
        }

        public static Common.NeuronResult ToCommon(this Domain.Model.Neuron value)
        {
            Common.NeuronResult result = new Common.NeuronResult();

            result.Id = value.Id;
            result.Tag = value.Tag;
            result.Creation = new AuthorEventInfo()
            {
                Timestamp = value.CreationTimestamp,
                Author = new NeuronInfo()
                {
                    Id = value.CreationAuthorId
                }
            };
            result.LastModification = new AuthorEventInfo()
            {
                Timestamp = value.LastModificationTimestamp,
                Author = new NeuronInfo()
                {
                    Id = value.LastModificationAuthorId
                }
            };
            result.UnifiedLastModification = new AuthorEventInfo()
            {
                Timestamp = value.UnifiedLastModificationTimestamp,
                Author = new NeuronInfo()
                {
                    Id = value.UnifiedLastModificationAuthorId
                }
            };
            result.Region = new NeuronInfo()
            {
                Id = value.RegionId
            };
            result.ExternalReferenceUrl = value.ExternalReferenceUrl;
            result.Version = value.Version;
            result.Active = value.Active;

            return result;
        }

        public static Common.Terminal ToCommon(this Domain.Model.Terminal value)
        {
            Common.Terminal result = new Common.Terminal();

            result.Id = value.Id;
            result.PresynapticNeuronId = value.PresynapticNeuronIdCore;
            result.PostsynapticNeuronId = value.PostsynapticNeuronIdCore;
            result.Effect = ((int)value.Effect).ToString();
            result.Strength = value.Strength.ToString();
            result.ExternalReferenceUrl = value.ExternalReferenceUrl;
            result.Version = value.Version;
            result.Creation = new AuthorEventInfo()
            {
                Timestamp = value.CreationTimestamp,
                Author = new NeuronInfo()
                {
                    Id = value.CreationAuthorId
                }
            };
            result.LastModification = new AuthorEventInfo()
            {
                Timestamp = value.LastModificationTimestamp,
                Author = new NeuronInfo()
                {
                    Id = value.LastModificationAuthorId
                }
            };
            result.Active = value.Active;

            return result;
        }

        public static Common.NeuronResult ToCommon(this Domain.Model.NeuronResult value, string centralId)
        {
            Common.NeuronResult result = null;

            if (value != null)
            {
                try
                {
                    if (value.Neuron != null || value.Terminal != null)
                    {
                        if (value.Neuron?.Id != null)
                        {
                            result = value.Neuron.ToCommon();
                            result.Creation.Author.Tag = value.NeuronCreationAuthorTag;
                            result.LastModification.Author.Tag = value.NeuronLastModificationAuthorTag;
                            result.UnifiedLastModification.Author.Tag = value.NeuronUnifiedLastModificationAuthorTag;
                            result.Region.Tag = value.NeuronRegionTag;
                        }

                        if (value.Terminal?.Id != null)
                        {
                            if (value.Neuron?.Id == null)
                            {
                                result = new Common.NeuronResult();

                                // If terminal is set but neuron is not set, terminal may be
                                // targetting a deactivated neuron
                                // or query is for Terminal only
                                if (!string.IsNullOrWhiteSpace(centralId))
                                {
                                    result.Tag = "[Not found]";
                                    result.Id = value.Terminal.PostsynapticNeuronIdCore.ToUpper() == centralId.ToUpper() ?
                                        value.Terminal.PresynapticNeuronIdCore :
                                        value.Terminal.PostsynapticNeuronIdCore;
                                }
                            }

                            result.Terminal = value.Terminal.ToCommon();
                            result.Terminal.Creation.Author.Tag = value.TerminalCreationAuthorTag;
                            result.Terminal.LastModification.Author.Tag = value.TerminalLastModificationAuthorTag;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"An exception occurred while converting Neuron '{value.Neuron.Tag}' (Id:{value.Neuron.Id}). Details:\n{ex.Message}", ex);
                }
            }

            return result;
        }
    }
}
