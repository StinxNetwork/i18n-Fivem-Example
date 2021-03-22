namespace I18NFivem.Contracts
{
    using System;
    using System.Collections.Generic;

    public interface ILocaleProvider : IDisposable
    {
        IEnumerable<Tuple<string, string>> GetAvailableLocales();
        string GetLocaleFileString(string locale);
        ILocaleProvider SetLogger(Action<string> logger);
        ILocaleProvider Init();
    }
}