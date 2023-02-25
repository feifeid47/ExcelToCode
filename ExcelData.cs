using System.Collections.Generic;
using Newtonsoft.Json;

namespace Feif
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ExcelTable
    {
        [JsonRequired]
        public string Type;
        [JsonRequired]
        public Dictionary<string, TypeValues> Fields = new Dictionary<string, TypeValues>();
    }
}