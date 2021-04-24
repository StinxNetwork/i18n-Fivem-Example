namespace I18NFivem
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using CitizenFX.Core;

    public static class Extensions
    {
        /// <summary>
        /// Get a translation from a key, formatting the string with the given params, if any
        /// </summary>
        public static string Translate(this string key, params object[] args)
        {
            return I18N.Current.Translate(key, args);
        }

        /// <summary>
        /// Get a translation from a key, formatting the string with the given params, if any. 
        /// It will return null when the translation is not found
        /// </summary>
        public static string TranslateOrNull(this string key, params object[] args)
        {
            return I18N.Current.TranslateOrNull(key, args);
        }

        public static string CapitalizeFirstCharacter(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            if (s.Length == 1)
                return s.ToUpper();

            return s.Remove(1).ToUpper() + s.Substring(1);
        }

        public static string UnescapeLineBreaks(this string str)
        {
            return str
                           .Replace("\\r\\n", "\\n")
                           .Replace("\\n", Environment.NewLine)
                           .Replace("\r\n", "\n")
                           .Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// Translates an Enum value.
        /// 
        /// i.e: <code>var dog = Animals.Dog.Translate()</code> will give "perro" if the the locale
        /// text file contains a line with "Animal.Dog = perro"
        /// </summary>
        public static string Translate(this Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetRuntimeField(value.ToString());
            string fieldName = fieldInfo.FieldType.Name;

            return $"{fieldName}.{value}".Translate();
        }

        public static string GetEnumDescription(this GtaLanguages value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
    }
}