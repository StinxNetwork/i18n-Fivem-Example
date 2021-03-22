namespace I18NFivem
{
    using System.Globalization;

    public partial class FivemCultureInfo
    {
        public string Name { get; }
        public string TwoLetterISOLanguageName { get; }

        public string EnglishName { get; }

        public FivemCultureInfo(string name, string twoLetterIsoLanguageName, string englishName)
        {
            Name = name;
            TwoLetterISOLanguageName = twoLetterIsoLanguageName;
            EnglishName = englishName;
        }

        public static FivemCultureInfo[] GetCultures()
        {
            return FivemCultureInfos.ToArray();
        }
    }
}