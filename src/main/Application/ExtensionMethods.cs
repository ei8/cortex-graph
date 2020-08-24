using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Application
{
    public static class ExtensionMethods
    {
        public static Common.Neuron ToCommon(this Domain.Model.Neuron value)
        {
            Common.Neuron result = new Common.Neuron();

            result.Id = value.Id;
            result.Tag = value.Tag;
            result.Timestamp = value.Timestamp;
            result.Version = value.Version;
            result.AuthorId = value.AuthorId;
            result.RegionId = value.RegionId;
            result.Active = value.Active;

            return result;
        }

        public static Common.Terminal ToCommon(this Domain.Model.Terminal value)
        {
            Common.Terminal result = new Common.Terminal();

            result.Id = value.Id;
            result.PresynapticNeuronId = value.PresynapticNeuronId;
            result.PostsynapticNeuronId = value.PostsynapticNeuronId;
            result.Effect = ((int)value.Effect).ToString();
            result.Strength = value.Strength.ToString();
            result.Version = value.Version;
            result.Timestamp = value.Timestamp;
            result.AuthorId = value.AuthorId;
            result.Active = value.Active;

            return result;
        }
    }
}
