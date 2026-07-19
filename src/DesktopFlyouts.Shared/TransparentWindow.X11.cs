#if HAS_UNO
using Uno.UI.Xaml;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DesktopFlyouts
{
    public partial class TransparentWindow : Window
    {
        public TransparentWindow()
        {
            WindowHelper.SetBackground(this, new SolidColorBrush(Microsoft.UI.Colors.Transparent));

            if (typeof(Window).GetProperty("RootElement", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this) is Panel root)
            {
                root.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

                root.ActualThemeChanged += delegate
                {
                    root.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                };

                CompositionTarget.Rendering += delegate {
                    if (root.Background is not SolidColorBrush s || s.Color != Microsoft.UI.Colors.Transparent)
                        root.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                };
            }
        }
    }
}
#endif
