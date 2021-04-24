namespace I18NFivem.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CitizenFX.Core.Native;
    using Contracts;

    public class FivemResourceProvider : ILocaleProvider
    {
        private readonly string _resourceName;
        private readonly string _folder;
        private readonly IEnumerable<string> _knownFileExtensions;
        private Action<string> _logger;
        private readonly Dictionary<string, string> _locales = new Dictionary<string, string>(); // ie: [es] = "Project.Locales.es.txt"

        public FivemResourceProvider(string resourceName, string folder, IEnumerable<string> knownFileExtensions)
        {
            _resourceName = resourceName;
            _folder = folder;
            _knownFileExtensions = knownFileExtensions;
        }

        public IEnumerable<Tuple<string, string>> GetAvailableLocales()
        {
            return _locales.Select(x =>
{
    string extension = x.Value.Substring(x.Value.LastIndexOf('.'));
    return new Tuple<string, string>(x.Key, extension);
});
        }

        public string GetLocaleFileString(string locale)
        {
            string resourcePath = _locales[locale];

            return API.LoadResourceFile(_resourceName, $"{_folder}/{resourcePath}");
        }

        public ILocaleProvider SetLogger(Action<string> logger)
        {
            _logger = logger;
            return this;
        }

        public ILocaleProvider Init()
        {
            DiscoverLocales();

            if (_locales?.Count == 0)
            {
                throw new I18NException($"{ErrorMessage.NoLocalesFound}: {_resourceName} {_folder}");
            }

            return this;
        }

        private void DiscoverLocales()
        {
            if (_logger != null)
            {
                _logger("Getting available locales...");

            }

            Dictionary<string, string> supportedLocaleAndExtensions;
            supportedLocaleAndExtensions = new Dictionary<string, string>();

            foreach (FivemCultureInfo cultureInfo in FivemCultureInfo.GetCultures())
            {
                bool found = false;

                foreach (string extension in _knownFileExtensions)
                {
                    string fileName = $"{cultureInfo.Name}{extension}";
                    string fileNameIso = $"{cultureInfo.TwoLetterISOLanguageName}{extension}";



                    if (!supportedLocaleAndExtensions.ContainsKey(cultureInfo.TwoLetterISOLanguageName) && IsResourceAvailable(fileNameIso))
                    {
                        supportedLocaleAndExtensions.Add(cultureInfo.TwoLetterISOLanguageName, fileNameIso);
                        found = true;

                    }
                    else if (!supportedLocaleAndExtensions.ContainsKey(cultureInfo.Name) && IsResourceAvailable(fileName))
                    {
                        supportedLocaleAndExtensions.Add(cultureInfo.Name, fileName);
                        found = true;
                    }

                    if (found)
                    {
                        break;
                    }
                }
            }

            if (supportedLocaleAndExtensions.Count == 0)
            {
                throw new I18NException("No locales have been found. Make sure you've got a folder " +
                                        $"called '{_folder}' containing embedded resource files " +
                                        $"(with extensions {string.Join(" or ", _knownFileExtensions)}) " +
                                        "in the host assembly");
            }


            foreach (KeyValuePair<string, string> localeAndExtension in supportedLocaleAndExtensions)
            {
                string localeName = localeAndExtension.Key;

                if (_locales.ContainsKey(localeName))
                {
                    throw new I18NException($"The locales folder '{_folder}' contains a duplicated locale '{localeName}'");
                }

                _locales.Add(localeName, localeAndExtension.Value);
            }

            if (_logger != null)
            {
                _logger($"Found {supportedLocaleAndExtensions.Count} locales: {string.Join(", ", _locales.Keys.ToArray())}");

            }

        }

        private bool IsResourceAvailable(string fileName)
        {
            string localeFileCultureName = API.LoadResourceFile(_resourceName, $"{_folder}/{fileName}");
            if (string.IsNullOrWhiteSpace(localeFileCultureName))
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            _locales.Clear();
            _logger = null;
        }
    }
}