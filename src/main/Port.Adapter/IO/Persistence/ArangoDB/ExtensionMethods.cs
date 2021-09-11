using ei8.Cortex.Graph.Common;
using ei8.Cortex.Graph.Domain.Model;
using neurUL.Common.Domain.Model;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace ei8.Cortex.Graph.Port.Adapter.IO.Persistence.ArangoDB
{
    public static class ExtensionMethods
    {
        internal static Domain.Model.Terminal CloneExcludeSynapticPrefix(this Domain.Model.Terminal terminal)
        {
            return new Domain.Model.Terminal(
                            terminal.Id,
                            terminal.PresynapticNeuronIdCore,
                            terminal.PostsynapticNeuronIdCore,
                            terminal.Effect,
                            terminal.Strength
                        )
            {
                ExternalReferenceUrl = terminal.ExternalReferenceUrl,
                Version = terminal.Version,
                CreationTimestamp = terminal.CreationTimestamp,
                CreationAuthorId = terminal.CreationAuthorId,
                LastModificationTimestamp = terminal.LastModificationTimestamp, 
                LastModificationAuthorId = terminal.LastModificationAuthorId,                
                Active = terminal.Active
            };
        }

        // TODO: Transfer to common
        public static string ToEnumString<T>(this T type)
            where T : Enum
        {
            var enumType = typeof(T);
            var name = Enum.GetName(enumType, type);
            var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
            return enumMemberAttribute.Value;
        }

        public static T ToEnum<T>(this string str)
            where T : Enum
        {
            var enumType = typeof(T);
            foreach (var name in Enum.GetNames(enumType))
            {
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                if (enumMemberAttribute.Value == str) return (T)Enum.Parse(enumType, name);
            }
            //throw exception or whatever handling you want or
            return default(T);
        }
    }
}
