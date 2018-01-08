using Newtonsoft.Json.Linq;
using org.neurul.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using works.ei8.Brain.Graph.Domain.Model;

namespace works.ei8.Brain.Graph.Port.Adapter.IO.Process.Events
{
    public class EventDataProcessor
    {
        public async Task<bool> Process(IRepository<NeuronVertex> repository, string eventName, string data)
        {
            bool result = false;

            JObject jd = JsonHelper.JObjectParse(data);
            NeuronVertex n = null;
            List<TerminalEdge> tlist = null;
            switch (eventName)
            {
                case "NeuronCreated":
                    n = new NeuronVertex()
                    {
                        Id = JsonHelper.GetRequiredValue<string>(jd, "Id"),
                        Data = JsonHelper.GetRequiredValue<string>(jd, "Data"),
                        Version = JsonHelper.GetRequiredValue<int>(jd, "Version"),
                        Timestamp = JsonHelper.GetRequiredValue<string>(jd, "TimeStamp"),
                    };
                    await repository.Save(n);
                    result = true;
                    break;
                case "TerminalsAdded":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, "Id")
                        ));

                    tlist = new List<TerminalEdge>(n.Terminals);
                    foreach (JToken to in JsonHelper.GetRequiredChildren(jd, "Terminals"))
                        tlist.Add(
                            new TerminalEdge(
                                Guid.NewGuid().ToString(),
                                n.Id,
                                JsonHelper.GetRequiredValue<string>(to, "Target")
                            )
                        );
                    n.Terminals = tlist.ToArray();
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, "Version");
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, "TimeStamp");

                    await repository.Save(n);
                    result = true;
                    break;
                case "NeuronDataChanged":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, "Id")
                        ));
                    n.Data = JsonHelper.GetRequiredValue<string>(jd, "Data");
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, "Version");
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, "TimeStamp");
                    await repository.Save(n);
                    result = true;
                    break;
                case "TerminalsRemoved":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, "Id")
                        ));

                    tlist = new List<TerminalEdge>(n.Terminals);
                    foreach (JToken to in JsonHelper.GetRequiredChildren(jd, "Terminals"))
                        tlist.RemoveAll(te => te.Target == JsonHelper.GetRequiredValue<string>(to, "Target"));
                    n.Terminals = tlist.ToArray();
                    n.Version = JsonHelper.GetRequiredValue<int>(jd, "Version");
                    n.Timestamp = JsonHelper.GetRequiredValue<string>(jd, "TimeStamp");

                    await repository.Save(n);
                    result = true;
                    break;
                case "NeuronDeactivated":
                    n = await repository.Get(Guid.Parse(
                        JsonHelper.GetRequiredValue<string>(jd, "Id")
                    ));

                    await repository.Remove(n);
                    result = true;
                    break;
            }

            return result;
        }
    }
}
