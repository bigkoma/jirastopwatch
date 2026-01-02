using System.Globalization;
using System.Resources;
using System.Reflection;

namespace StopWatch.Localization
{
    internal static class Localizer
    {
        private static readonly ResourceManager _rm = new ResourceManager("StopWatch.Resources.Strings", Assembly.GetExecutingAssembly());
        public static CultureInfo Culture { get; set; } = CultureInfo.CurrentUICulture;

        public static void InitializeCulture()
        {
            var settings = Settings.Instance;
            if (!string.IsNullOrEmpty(settings.LanguageCode))
            {
                try
                {
                    Culture = new CultureInfo(settings.LanguageCode);
                }
                catch
                {
                    Culture = CultureInfo.CurrentUICulture;
                }
            }
            else
            {
                Culture = CultureInfo.CurrentUICulture;
            }
        }

        public static string T(string key)
        {
            try { return _rm.GetString(key, Culture) ?? key; }
            catch { return key; }
        }
    }
}
