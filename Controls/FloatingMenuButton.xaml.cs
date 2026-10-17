namespace Servus.Mobile.Controls;

// Lives in the app-wide ContentPage ControlTemplate (App.xaml), next to the page's
// ContentPresenter, so every screen gets it without touching the page itself.
// Sits at the top-left. Hides while the page scrolls down and returns on scroll up
// (Material's hide-on-scroll), so it doesn't float over the content being read.
public partial class FloatingMenuButton : ContentView
{
    private const double ScrollThreshold = 6;
    private const double HiddenOffset = -120;

    // Pages without the Shell nav bar (the Home) start under the status bar and draw their own
    // header, which leaves this slot free; the button is aligned with that header row.
    private static readonly Thickness NoNavBarMargin = new(16, 43, 0, 0);

    // Under a nav bar there is no free slot: the page content is pushed down by
    // ContentInsetWithNavBar so the button gets its own strip instead of covering the first row.
    private static readonly Thickness NavBarMargin = new(16, 10, 0, 0);
    private const double ContentInsetWithNavBar = 64;

    private readonly Dictionary<ScrollView, double> _lastScrollY = [];
    private readonly List<Action> _unsubscribe = [];
    private bool _hidden;

    public FloatingMenuButton()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Shell.Current is { } shell)
        {
            shell.FlyoutIsPresented = true;
        }
    }

    // Also fires when a page in the back stack becomes visible again: a button left hidden by
    // the last scroll there would otherwise come back hidden, with the menu out of reach.
    private void OnLoaded(object? sender, EventArgs e)
    {
        OnUnloaded(sender, e);
        ResetVisible();

        if (Parent is not Layout host) return;

        var hasNavBar = FindPage() is { } page && Shell.GetNavBarIsVisible(page);
        Margin = hasNavBar ? NavBarMargin : NoNavBarMargin;

        foreach (var presenter in host.Children.OfType<ContentPresenter>())
        {
            presenter.Margin = new Thickness(0, hasNavBar ? ContentInsetWithNavBar : 0, 0, 0);

            foreach (var element in Descendants(presenter))
            {
                switch (element)
                {
                    case ScrollView scrollView when scrollView.Orientation != ScrollOrientation.Horizontal:
                        scrollView.Scrolled += OnScrollViewScrolled;
                        _unsubscribe.Add(() => scrollView.Scrolled -= OnScrollViewScrolled);
                        break;
                    case ItemsView itemsView:
                        itemsView.Scrolled += OnItemsViewScrolled;
                        _unsubscribe.Add(() => itemsView.Scrolled -= OnItemsViewScrolled);
                        break;
                }
            }
        }
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        foreach (var unsubscribe in _unsubscribe)
        {
            unsubscribe();
        }

        _unsubscribe.Clear();
        _lastScrollY.Clear();
    }

    private void OnScrollViewScrolled(object? sender, ScrolledEventArgs e)
    {
        if (sender is not ScrollView scrollView) return;

        var last = _lastScrollY.GetValueOrDefault(scrollView);
        _lastScrollY[scrollView] = e.ScrollY;

        // Back at the top always shows it: an overscroll bounce can end with a downward delta.
        if (e.ScrollY <= 0)
        {
            SetHidden(false);
            return;
        }

        React(e.ScrollY - last);
    }

    // Horizontal lists (e.g. the coverage strip) report VerticalDelta 0 and fall through React.
    private void OnItemsViewScrolled(object? sender, ItemsViewScrolledEventArgs e) => React(e.VerticalDelta);

    private void React(double delta)
    {
        if (delta > ScrollThreshold) SetHidden(true);
        else if (delta < -ScrollThreshold) SetHidden(false);
    }

    private void SetHidden(bool hidden)
    {
        if (_hidden == hidden) return;
        _hidden = hidden;

        // On the inner Border, never on this view: toggling InputTransparent on a ContentView makes
        // Android wrap/unwrap its native view, which fires Unloaded/Loaded and wiped the scroll
        // positions mid-gesture, so a scroll up never registered and the button stayed hidden.
        Button.InputTransparent = hidden;
        this.CancelAnimations();
        _ = hidden
            ? Task.WhenAll(this.TranslateToAsync(0, HiddenOffset, 180, Easing.CubicIn), this.FadeToAsync(0, 180))
            : Task.WhenAll(this.TranslateToAsync(0, 0, 220, Easing.CubicOut), this.FadeToAsync(1, 220));
    }

    private Page? FindPage()
    {
        Element? element = Parent;
        while (element is not null and not Page)
        {
            element = element.Parent;
        }

        return element as Page;
    }

    private void ResetVisible()
    {
        this.CancelAnimations();
        _hidden = false;
        Button.InputTransparent = false;
        TranslationY = 0;
        Opacity = 1;
    }

    private static IEnumerable<IVisualTreeElement> Descendants(IVisualTreeElement root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
