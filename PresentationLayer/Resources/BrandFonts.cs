using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationLayer.Resources
{
    /// <summary>
    /// Makes the graphic profile's body font (Open Sans) the default font for every window.
    /// Headings use Montserrat through HeadingFont/HeadingTextStyle in theme.xaml.
    /// </summary>
    public static class BrandFonts
    {
        private static bool _applied;

        // Makes sure the "pack://" address scheme is registered before the font addresses are created
        private static readonly string PackScheme = System.IO.Packaging.PackUriHelper.UriSchemePack;

        public static FontFamily Body { get; } =
            new FontFamily(new Uri("pack://application:,,,/PresentationLayer;component/Resources/Fonts/"), "./#Open Sans");

        public static FontFamily Heading { get; } =
            new FontFamily(new Uri("pack://application:,,,/PresentationLayer;component/Resources/Fonts/"), "./#Montserrat");

        /// <summary>
        /// Must run before the first window is created. Text inside a window inherits the window's font.
        /// </summary>
        public static void Apply()
        {
            if (_applied)
            {
                return;
            }

            Control.FontFamilyProperty.OverrideMetadata(
                typeof(Window),
                new FrameworkPropertyMetadata(Body, FrameworkPropertyMetadataOptions.Inherits));

            _applied = true;
        }
    }
}
