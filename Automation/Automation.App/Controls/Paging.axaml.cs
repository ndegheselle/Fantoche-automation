using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Automation.App.Controls
{
    /// <summary>
    /// MIGRATION: reusable pager (replaces Joufflu.Data.Paging). Exposes Total / PageNumber /
    /// Capacity and raises <see cref="PagingChange"/> on user navigation. Used by ScopeHistory,
    /// task history and the packages list. PagingChange fires only on user gestures (buttons /
    /// capacity change) to avoid feedback loops when the host reloads and resets Total/PageNumber.
    /// </summary>
    public partial class Paging : UserControl
    {
        public static readonly StyledProperty<int> TotalProperty =
            AvaloniaProperty.Register<Paging, int>(nameof(Total));

        public static readonly StyledProperty<int> PageNumberProperty =
            AvaloniaProperty.Register<Paging, int>(nameof(PageNumber), defaultValue: 1);

        public static readonly StyledProperty<int> CapacityProperty =
            AvaloniaProperty.Register<Paging, int>(nameof(Capacity), defaultValue: 50);

        public static readonly StyledProperty<int> PageMaxProperty =
            AvaloniaProperty.Register<Paging, int>(nameof(PageMax), defaultValue: 1);

        public static readonly StyledProperty<int> IntervalMinProperty =
            AvaloniaProperty.Register<Paging, int>(nameof(IntervalMin));

        public static readonly StyledProperty<int> IntervalMaxProperty =
            AvaloniaProperty.Register<Paging, int>(nameof(IntervalMax));

        public int Total { get => GetValue(TotalProperty); set => SetValue(TotalProperty, value); }
        public int PageNumber { get => GetValue(PageNumberProperty); set => SetValue(PageNumberProperty, value); }
        public int Capacity { get => GetValue(CapacityProperty); set => SetValue(CapacityProperty, value); }
        public int PageMax { get => GetValue(PageMaxProperty); private set => SetValue(PageMaxProperty, value); }
        public int IntervalMin { get => GetValue(IntervalMinProperty); private set => SetValue(IntervalMinProperty, value); }
        public int IntervalMax { get => GetValue(IntervalMaxProperty); private set => SetValue(IntervalMaxProperty, value); }

        public List<int> AvailableCapacities { get; } = new() { 5, 10, 25, 50, 100, 200 };

        /// <summary>Raised on user navigation: (pageNumber, capacity).</summary>
        public event Action<int, int>? PagingChange;

        public Paging()
        {
            InitializeComponent();
            Recompute();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == TotalProperty || change.Property == CapacityProperty || change.Property == PageNumberProperty)
                Recompute();
        }

        private void Recompute()
        {
            int pageMax = Total <= 0 ? Math.Max(1, PageNumber) : Math.Max(1, (int)Math.Ceiling(Total / (double)Capacity));
            PageMax = pageMax;

            int min = Capacity * (PageNumber - 1) + 1;
            IntervalMin = Total <= 0 ? 0 : min;
            IntervalMax = Total <= 0 ? 0 : Math.Min(min + Capacity - 1, Total);
        }

        private void GoTo(int page)
        {
            int clamped = Math.Clamp(page, 1, Math.Max(1, PageMax));
            if (clamped == PageNumber)
                return;
            PageNumber = clamped;
            PagingChange?.Invoke(PageNumber, Capacity);
        }

        private void First_Click(object? sender, RoutedEventArgs e) => GoTo(1);
        private void Previous_Click(object? sender, RoutedEventArgs e) => GoTo(PageNumber - 1);
        private void Next_Click(object? sender, RoutedEventArgs e) => GoTo(PageNumber + 1);
        private void Last_Click(object? sender, RoutedEventArgs e) => GoTo(PageMax);

        private void Capacity_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // Reset to the first page when the page size changes, then notify.
            PageNumber = 1;
            Recompute();
            PagingChange?.Invoke(PageNumber, Capacity);
        }
    }
}
