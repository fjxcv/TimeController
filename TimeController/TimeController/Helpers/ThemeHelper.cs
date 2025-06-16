using System;
using System.Windows;
using Microsoft.Win32;
using TimeController.Services;

namespace TimeController.Helpers
{
    public static class ThemeHelper
    {
        public static void ApplyAppTheme(bool isLight)
        {
            var uri = new Uri($"/TimeController;component/Themes/{(isLight ? "Light" : "Dark") }Theme.xaml", UriKind.Relative);
            var dict = new ResourceDictionary { Source = uri };
            Application.Current.Resources.MergedDictionaries[0] = dict;
        }

        public static bool GetSystemIsLight()
        {
            const string key = @"HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
            var value = Registry.GetValue(key, "AppsUseLightTheme", 1);
            return Convert.ToInt32(value) == 1;
        }

        public static void ApplyFromSettings(ISettingsService settings)
        {
            var opt = settings.LoadThemeOption();
            if (opt == ThemeOption.System)
            {
                ApplyAppTheme(GetSystemIsLight());
            }
            else
            {
                ApplyAppTheme(opt == ThemeOption.Light);
            }
        }
    }
}
