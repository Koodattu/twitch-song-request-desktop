using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace TwitchSongRequest.Helpers
{
    internal class SliderExtension
    {
        public static readonly DependencyProperty DragCompletedCommandProperty = DependencyProperty.RegisterAttached(
            "DragCompletedCommand",
            typeof(ICommand),
            typeof(SliderExtension),
            new PropertyMetadata(default(ICommand), OnDragCompletedCommandChanged));

        private static void OnDragCompletedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Slider slider = d as Slider;
            if (slider == null)
            {
                return;
            }

            if (e.NewValue is ICommand)
            {
                slider.Loaded += SliderOnLoaded;
            }
        }

        private static void SliderOnLoaded(object sender, RoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider == null)
            {
                return;
            }
            slider.Loaded -= SliderOnLoaded;

            Track track = slider.Template.FindName("PART_Track", slider) as Track;
            if (track == null)
            {
                return;
            }
            track.Thumb.DragCompleted += (dragCompletedSender, dragCompletedArgs) =>
            {
                ICommand command = GetDragCompletedCommand(slider);
                command.Execute(null);
            };
        }

        public static void SetDragCompletedCommand(DependencyObject element, ICommand value)
        {
            element.SetValue(DragCompletedCommandProperty, value);
        }

        public static ICommand GetDragCompletedCommand(DependencyObject element)
        {
            return (ICommand)element.GetValue(DragCompletedCommandProperty);
        }
    }
}