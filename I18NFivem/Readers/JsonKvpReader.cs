namespace I18NFivem.Readers
{
    using System.Collections.Generic;
    using System.Linq;
    using CitizenFX.Core;
    using Contracts;
    using Newtonsoft.Json;

    public class JsonKvpReader : ILocaleReader
    {
        public Dictionary<string, string> Read(string file)
        {
            return JsonConvert
                .DeserializeObject<Dictionary<string, string>>(file)
                .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim().UnescapeLineBreaks());
        }
    }
}