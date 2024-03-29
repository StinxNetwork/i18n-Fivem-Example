﻿namespace I18NFivem
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using CitizenFX.Core;
    using CitizenFX.Core.Native;
    using Contracts;
    using Providers;

    public class I18N : II18N
    {
        private string _notFoundSymbol = "?";
        private string _fallbackLocale;
        private string _resourcesFolder;
        private string _locale;
        private bool _throwWhenKeyNotFound;
        private Action<string> _logger;
        private Dictionary<string, string> _localeFileExtensionMap;
        private Dictionary<string, string> _translations = new Dictionary<string, string>();
        private IList<string> _locales = new List<string>();

        private readonly IList<Tuple<ILocaleReader, string>> _readers = new List<Tuple<ILocaleReader, string>>();
        private readonly IList<ILocaleProvider> _providers = new List<ILocaleProvider>();
        private GtaLanguages ActiveGtaLanguage = (GtaLanguages)API.GetCurrentLanguage();

        public static I18N Current { get; set; } = new I18N();

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.DynamicInvoke(this, new PropertyChangedEventArgs(info));
        }


        /// <summary>
        /// Use the indexer to translate keys. If you need string formatting, use <code>Translate()</code> instead
        /// </summary>
        public string this[string key] => Translate(key);

        /// <summary>
        /// The current loaded Language, if any
        /// </summary>
        public Language Language
        {
            get => Languages?.FirstOrDefault(x => x.Locale.Equals(Locale));
            set
            {
                if (Language.Locale == value.Locale)
                {
                    Log($"{value.DisplayName} is the current language. No actions will be taken");
                    return;
                }

                LoadLocale(value.Locale);

                NotifyPropertyChanged(nameof(Locale));
                NotifyPropertyChanged(nameof(Language));
            }
        }

        public string Locale
        {
            get => _locale;
            set
            {
                if (_locale == value)
                {
                    Log($"{value} is the current locale. No actions will be taken");
                    return;
                }

                LoadLocale(value);

                NotifyPropertyChanged(nameof(Locale));
                NotifyPropertyChanged(nameof(Language));
            }
        }

        public List<Language> Languages => _locales?.Select(x => new Language
        {
            Locale = x,
            DisplayName = TranslateOrNull(x) ?? new CultureInfo(x).NativeName.CapitalizeFirstCharacter()
        })
            .ToList();

        /// <summary>
        /// Set the symbol to wrap a key when not found. ie: if you set "##", a not found key will
        /// be translated as "##key##". 
        /// The default symbol is "?"
        /// </summary>
        public II18N SetNotFoundSymbol(string symbol)
        {
            if (!string.IsNullOrEmpty(symbol))
            {
                _notFoundSymbol = symbol;
            }

            return this;
        }

        /// <summary>
        /// Enable I18N logs with an action
        /// </summary>
        /// <param name="output">Action to be invoked as the output of the logger</param>
        public II18N SetLogger(Action<string> output)
        {
            _logger = output;
            return this;
        }

        /// <summary>
        /// Throw an exception whenever a key is not found in the locale file (fail early, fail fast)
        /// </summary>
        public II18N SetThrowWhenKeyNotFound(bool enabled)
        {
            _throwWhenKeyNotFound = enabled;
            return this;
        }

        /// <summary>
        /// Set the locale that will be loaded in case the system language is not supported
        /// </summary>
        public II18N SetFallbackLocale(string locale)
        {
            _fallbackLocale = locale;
            return this;
        }

        public II18N SetResourcesFolder(string folderName)
        {
            _resourcesFolder = folderName;
            return this;
        }

        public II18N AddLocaleReader(ILocaleReader reader, string extension)
        {
            if (reader == null)
            {
                throw new I18NException(ErrorMessage.ReaderNull);
            }

            if (string.IsNullOrEmpty(extension))
                throw new I18NException(ErrorMessage.ReaderExtensionNeeded);

            if (!extension.StartsWith("."))
                throw new I18NException(ErrorMessage.ReaderExtensionStartWithDot);

            if (extension.Length < 2)
                throw new I18NException(ErrorMessage.ReaderExtensionOneChar);

            if (extension.Split('.').Length - 1 > 1)
                throw new I18NException(ErrorMessage.ReaderExtensionJustOneDot);

            if (_readers.Any(x => x.Item2.Equals(extension)))
                throw new I18NException(ErrorMessage.ReaderExtensionTwice);

            if (_readers.Any(x => x.Item1 == reader))
                throw new I18NException(ErrorMessage.ReaderTwice);

            _readers.Add(new Tuple<ILocaleReader, string>(reader, extension));

            return this;
        }

        /// <summary>
        /// Call this when your app starts
        /// ie: <code>I18N.Current.Init(API.GetResourceName);</code>
        /// </summary>
        /// <param name="resourceName">The assembly that hosts the locale text files</param>
        public II18N Init(string resourceName)
        {
            IEnumerable<string> knowFileExtensions = _readers.Select(x => x.Item2);

            foreach (ILocaleProvider provider in _providers)
            {
                provider.Dispose();
            }

            _providers.Clear();

            if (_providers.FirstOrDefault(x => x is FivemResourceProvider) == null)
            {
                string resourcesFolder = _resourcesFolder ?? "Locales";
                ILocaleProvider defaultProvider = new FivemResourceProvider(resourceName, resourcesFolder, knowFileExtensions)
                    .SetLogger(Log)
                    .Init();

                _providers.Add(defaultProvider);
            }

            List<Tuple<string, string>> localeTuples = _providers.First().GetAvailableLocales().ToList();
            _locales = localeTuples.Select(x => x.Item1).ToList();
            _localeFileExtensionMap = localeTuples.ToDictionary(x => x.Item1, x => x.Item2);

            string location = GetLocationAndLoad();

            Log($"{location} was loaded");

            NotifyPropertyChanged(nameof(Locale));
            NotifyPropertyChanged(nameof(Language));

            return this;
        }

        private string GetLocationAndLoad()
        {
            string localeToLoad = GetDefaultLocale();

            if (string.IsNullOrEmpty(localeToLoad))
            {
                if (!string.IsNullOrEmpty(_fallbackLocale) && _locales.Contains(_fallbackLocale))
                {
                    localeToLoad = _fallbackLocale;
                    Log($"Loading fallback locale: {_fallbackLocale}");
                }
                else
                {
                    localeToLoad = _locales.ElementAt(0);
                    Log($"Loading first locale on the list: {localeToLoad}");
                }
            }
            else
            {
                Log($"Default locale from current culture: {localeToLoad}");
            }

            LoadLocale(localeToLoad);

            return localeToLoad;
        }

        private void LoadLocale(string locale)
        {
            Debug.WriteLine(locale);
            if (!_locales.Contains(locale))
            {
                throw new I18NException($"Locale '{locale}' is not available", new KeyNotFoundException());
            }

            string localeFileString = _providers.First().GetLocaleFileString(locale);

            _translations.Clear();

            string extension = _localeFileExtensionMap[locale];
            ILocaleReader reader = _readers.First(x => x.Item2.Equals(extension)).Item1;

            try
            {
                _translations = reader.Read(localeFileString) ?? new Dictionary<string, string>();
            }
            catch (Exception e)
            {
                string message =
                    $"{ErrorMessage.ReaderException}.\nReader: {reader.GetType().Name}.\nLocale: {locale}{extension}";
                throw new I18NException(message, e);
            }

            LogTranslations();

            _locale = locale;


            // Update bindings to indexer (useful for MVVM)
            NotifyPropertyChanged("Item[]");
        }

        public string GetDefaultLocale()
        {
            GtaLanguages currentLanguage = (GtaLanguages)API.GetCurrentLanguage();
            string enumDescription = currentLanguage.GetEnumDescription();

            FivemCultureInfo currentCulture = FivemCultureInfo.FivemCultureInfos.First(x =>
                x.TwoLetterISOLanguageName == enumDescription || x.Name == enumDescription.Split('-')[0]);


            // only available in runtime (not from PCL) on the simulator
            // var threeLetterIsoName = currentCulture.GetType().GetRuntimeProperty("ThreeLetterISOLanguageName").GetValue(currentCulture);
            // var threeLetterWindowsName = currentCulture.GetType().GetRuntimeProperty("ThreeLetterWindowsLanguageName").GetValue(currentCulture);

            string matchingLocale = _locales.FirstOrDefault(x => x.Equals(currentCulture.Name));

            if (matchingLocale == null)
            {
                matchingLocale = _locales.FirstOrDefault(x => x.Equals(currentCulture.TwoLetterISOLanguageName));
            }

            return matchingLocale;

            // ISO 639-1 two-letter code. i.e: "es"
            // || x.Key.Equals(threeLetterIsoName) // ISO 639-2 three-letter code. i.e: "spa"
            // || x.Key.Equals(threeLetterWindowsName)); // "ESP"
        }

        /// <summary>
        /// Get a translation from a key, formatting the string with the given params, if any
        /// </summary>
        public string Translate(string key, params object[] args)
        {
            ValidateLocale();

            if (_translations.ContainsKey(key))
                return args.Length == 0
                    ? _translations[key]
                    : string.Format(_translations[key], args);

            if (_throwWhenKeyNotFound)
                throw new KeyNotFoundException(
                    $"[{nameof(I18N)}] key '{key}' not found in the current language '{_locale}'");

            return $"{_notFoundSymbol}{key}{_notFoundSymbol}";
        }

        private void ValidateLocale()
        {
            GtaLanguages currentLanguage = (GtaLanguages)API.GetCurrentLanguage();

            if (ActiveGtaLanguage != currentLanguage)
            {
                GetLocationAndLoad();
                ActiveGtaLanguage = currentLanguage;
            }
        }

        /// <summary>
        /// Get a translation from a key, formatting the string with the given params, if any. 
        /// It will return null when the translation is not found
        /// </summary>
        public string TranslateOrNull(string key, params object[] args)
        {
            return _translations.ContainsKey(key)
? (args.Length == 0 ? _translations[key] : string.Format(_translations[key], args))
: null;
        }

        /// <summary>
        /// Convert Enum Type values to a Dictionary&lt;TEnum, string&gt; where the key is the Enum value and the string is the translated value.
        /// </summary>
        public Dictionary<TEnum, string> TranslateEnumToDictionary<TEnum>()
        {
            Type type = typeof(TEnum);
            Dictionary<TEnum, string> dic = new Dictionary<TEnum, string>();

            foreach (object value in Enum.GetValues(type))
            {
                string name = Enum.GetName(type, value);
                dic.Add((TEnum)value, Translate($"{type.Name}.{name}"));
            }

            return dic;
        }


        /// <summary>
        /// Convert Enum Type values to a List of translated strings
        /// </summary>
        public List<string> TranslateEnumToList<TEnum>()
        {
            Type type = typeof(TEnum);

            return (from object value in Enum.GetValues(type)
                    select Enum.GetName(type, value)
                    into name
                    select Translate($"{type.Name}.{name}"))
                .ToList();
        }

        /// <summary>
        /// Converts Enum Type values to a List of <code>Tuple&lt;TEnum, string&gt;</code> where the Item2 (string) is the enum value translation
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public List<Tuple<TEnum, string>> TranslateEnumToTupleList<TEnum>()
        {
            Type type = typeof(TEnum);
            List<Tuple<TEnum, string>> list = new List<Tuple<TEnum, string>>();

            foreach (object value in Enum.GetValues(type))
            {
                string name = Enum.GetName(type, value);
                Tuple<TEnum, string> tuple = new Tuple<TEnum, string>((TEnum)value, Translate($"{type.Name}.{name}"));
                list.Add(tuple);
            }

            return list;
        }

        public void Dispose()
        {
            if (PropertyChanged != null)
            {
                foreach (Delegate @delegate in PropertyChanged.GetInvocationList())
                {
                    PropertyChanged -= (PropertyChangedEventHandler)@delegate;
                }

                PropertyChanged = null;
            }

            _translations?.Clear();
            _locales?.Clear();
            _readers?.Clear();
            _localeFileExtensionMap?.Clear();
            _locale = null;

            Current = null;

            Log("Disposed");

            _logger = null;
        }


        private void LogTranslations()
        {
            Log("========== I18N Fivem translations ==========");
            foreach (KeyValuePair<string, string> item in _translations)
            {
                Log($"{item.Key} = {item.Value}");
            }

            Log("====== I18N Fivem end of translations =======");
        }

        private void Log(string trace)
        {
            _logger?.DynamicInvoke($"[{nameof(I18N)}] {trace}");
        }
    }
}