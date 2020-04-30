using ArangoDB.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Cortex.Graph.Domain.Model
{
    public class Settings
    {
        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id
        {
            get
            {
                return Guid.Empty.ToString();
            }
            set
            {
            }
        }

        public string LastPosition { get; set; }
    }
}
