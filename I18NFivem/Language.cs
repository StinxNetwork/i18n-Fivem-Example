namespace I18NFivem
{
    public class Language
    {
        public string Locale { get; set; }
        public string DisplayName { get; set; }
        public override string ToString() => DisplayName;
    }
}