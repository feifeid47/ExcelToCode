using System.Collections.Generic;
using Newtonsoft.Json;

namespace Feif
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TypeValues
    {
        public string Define;
        [JsonRequired]
        public string Type;
        [JsonRequired]
        public List<object> Values = new List<object>();
    }
}