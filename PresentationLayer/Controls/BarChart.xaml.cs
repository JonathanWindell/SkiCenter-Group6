using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PresentationLayer.Controls
{
    /// <summary>
    /// One group of bars: the value for the selected period and, optionally, the same month the year before.
    /// </summary>
    public record BarChartPoint(string Label, double Current, double? Previous);

    /// <summary>
    /// Simple grouped bar chart drawn with WPF elements, so no chart package is needed.
    /// </summary>
    public partial class BarChart : UserControl
    {
        private static readonly Brush PreviousBrush = new SolidColorBrush(Color.FromRgb(0xA5, 0xB4, 0xFC));

        public BarChart()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => TitleTextBlock.Text;
            set => TitleTextBlock.Text = value;
        }

        /// <summary>
        /// Draws the chart.
        /// </summary>
        /// <param name="points">One point per month.</param>
        /// <param name="formatValue">Formats a value for labels and tooltips.</param>
        /// <param name="maxValue">Fixed top of the scale, e.g. 1 for occupancy. Null scales to the largest value.</param>
        /// <param name="showPrevious">Whether to show the previous year's bars.</param>
        public void SetData(IReadOnlyList<BarChartPoint> points, Func<double, string> formatValue, double? maxValue, bool showPrevious)
        {
            ChartGrid.Children.Clear();
            ChartGrid.ColumnDefinitions.Clear();

            PreviousLegendRectangle.Visibility = showPrevious ? Visibility.Visible : Visibility.Collapsed;
            PreviousLegendTextBlock.Visibility = showPrevious ? Visibility.Visible : Visibility.Collapsed;

            double largest = points.Count == 0
                ? 0
                : points.Max(p => Math.Max(p.Current, showPrevious ? p.Previous ?? 0 : 0));
            double scaleMax = maxValue ?? largest;

            bool hasData = points.Count > 0 && (largest > 0 || maxValue.HasValue);
            EmptyTextBlock.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;

            if (!hasData || scaleMax <= 0)
            {
                return;
            }

            Brush currentBrush = (Brush)FindResource("PrimaryBtn");
            Brush labelBrush = (Brush)FindResource("PrimaryText");

            for (int i = 0; i < points.Count; i++)
            {
                BarChartPoint point = points[i];
                ChartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var group = new Grid { Margin = new Thickness(4, 0, 4, 0) };
                group.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                group.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                group.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var valueLabel = new TextBlock
                {
                    Text = formatValue(point.Current),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = labelBrush,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                Grid.SetRow(valueLabel, 0);
                group.Children.Add(valueLabel);

                var bars = new Grid();
                bars.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                if (showPrevious)
                {
                    bars.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                bars.Children.Add(CreateBar(point.Current, scaleMax, currentBrush, $"{point.Label}: {formatValue(point.Current)}", 0));

                if (showPrevious)
                {
                    double previous = point.Previous ?? 0;
                    bars.Children.Add(CreateBar(previous, scaleMax, PreviousBrush, $"{point.Label} föregående år: {formatValue(previous)}", 1));
                }

                Grid.SetRow(bars, 1);
                group.Children.Add(bars);

                var monthLabel = new TextBlock
                {
                    Text = point.Label,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = labelBrush,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                Grid.SetRow(monthLabel, 2);
                group.Children.Add(monthLabel);

                Grid.SetColumn(group, i);
                ChartGrid.Children.Add(group);
            }
        }

        /// <summary>
        /// A bar whose height is its share of the scale, using star rows so it scales with the control.
        /// </summary>
        private static Grid CreateBar(double value, double scaleMax, Brush fill, string tooltip, int column)
        {
            double share = Math.Clamp(value / scaleMax, 0, 1);

            var holder = new Grid { Margin = new Thickness(1, 0, 1, 0), ToolTip = tooltip, Background = Brushes.Transparent };
            holder.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1 - share, GridUnitType.Star) });
            holder.RowDefinitions.Add(new RowDefinition { Height = new GridLength(share, GridUnitType.Star) });

            var bar = new Border
            {
                Background = fill,
                CornerRadius = new CornerRadius(3, 3, 0, 0),
                MinHeight = value > 0 ? 2 : 0
            };
            Grid.SetRow(bar, 1);
            holder.Children.Add(bar);

            Grid.SetColumn(holder, column);
            return holder;
        }
    }
}
