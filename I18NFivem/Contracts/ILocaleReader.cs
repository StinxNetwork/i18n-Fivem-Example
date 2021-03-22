namespace I18NFivem.Contracts
{
    using System.Collections.Generic;

    public interface ILocaleReader
    {
        Dictionary<string, string> Read(string file);
    }
}