using Newtonsoft.Json;

namespace Feif
{
    public class Setting
    {
        [JsonIgnore]
        public static Setting Default { get; set; }

        public string Arg1;
        public string Arg2;
        public string Arg3;
        public string Arg4;
        public string Arg5;
        public string Arg6;
        public string Arg1Info;
        public string Arg2Info;
        public string Arg3Info;
        public string Arg4Info;
        public string Arg5Info;
        public string Arg6Info;
        public string DefaultNamespace;
        public string DefaultSuffix;
        public string ReplaceNamespace;
        public string ReplaceProperties;
        public string ReplaceClass;
        public string ReplaceType;
        public string ReplaceName;
        public string TemplateFileName1;
        public string TemplateFileName2;
        public string TemplateFileName3;
        public string TemplateFileName4;
        public string CatalogName;
        public string Key1;
        public string Key2;
        public string Key3;
        public string Key4;
        public string Key5;
        public string Lang1;
        public string Lang2;
        public string Lang3;
        public string Lang4;
        public string Lang5;
    }
}