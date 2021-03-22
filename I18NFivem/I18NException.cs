namespace I18NFivem
{
    using System;

    public class I18NException : Exception
    {
        public I18NException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}