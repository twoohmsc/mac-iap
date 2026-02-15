using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;

namespace IapDesktop.Application.Avalonia.Converters
{
    public class ImageResourceConverter : IValueConverter
    {
        public static ImageResourceConverter Instance = new ImageResourceConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("avares://"))
                {
                    try
                    {
                        Console.WriteLine($"DEBUG: Loading asset {path}");
                        var assets = AssetLoader.Open(new Uri(path));
                        var bitmap = new Bitmap(assets);
                        Console.WriteLine($"DEBUG: Loaded bitmap {path} ({bitmap.Size.Width}x{bitmap.Size.Height})");
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Failed to load asset {path}: {ex.Message}");
                        return null;
                    }
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
