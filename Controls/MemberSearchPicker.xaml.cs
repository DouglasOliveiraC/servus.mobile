using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Servus.Mobile.Controls;

// One block for "search a member, tap to pick" (roster Adicionar / Conceder acesso, day edit
// Trocar). Each screen keeps its own view model and search state; this control only joins the
// search text, the results and the select command so the screens can't drift apart visually.
//
// Results render as a plain capped list, not a CollectionView: every host page already scrolls,
// and a fixed-height CollectionView inside that scroll showed ~3 names and barely scrolled on
// Android. Past MaxVisibleResults the user narrows the list by typing instead.
public partial class MemberSearchPicker : ContentView
{
    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(MemberSearchPicker), "Buscar membro");

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(MemberSearchPicker), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty ResultsProperty =
        BindableProperty.Create(nameof(Results), typeof(IEnumerable), typeof(MemberSearchPicker),
            propertyChanged: (bindable, oldValue, newValue) => ((MemberSearchPicker)bindable).OnResultsChanged(oldValue as IEnumerable, newValue as IEnumerable));

    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(MemberSearchPicker));

    public static readonly BindableProperty MaxVisibleResultsProperty =
        BindableProperty.Create(nameof(MaxVisibleResults), typeof(int), typeof(MemberSearchPicker), 6,
            propertyChanged: (bindable, _, _) => ((MemberSearchPicker)bindable).RefreshVisibleResults());

    public MemberSearchPicker()
    {
        InitializeComponent();
    }

    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public IEnumerable Results
    {
        get => (IEnumerable)GetValue(ResultsProperty);
        set => SetValue(ResultsProperty, value);
    }

    public ICommand SelectCommand
    {
        get => (ICommand)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public int MaxVisibleResults
    {
        get => (int)GetValue(MaxVisibleResultsProperty);
        set => SetValue(MaxVisibleResultsProperty, value);
    }

    public ObservableCollection<object> VisibleResults { get; } = [];

    public string OverflowText { get; private set; } = string.Empty;

    public bool HasOverflow { get; private set; }

    private void OnResultsChanged(IEnumerable? oldValue, IEnumerable? newValue)
    {
        if (oldValue is INotifyCollectionChanged oldObservable)
        {
            oldObservable.CollectionChanged -= OnResultsCollectionChanged;
        }

        if (newValue is INotifyCollectionChanged newObservable)
        {
            newObservable.CollectionChanged += OnResultsCollectionChanged;
        }

        RefreshVisibleResults();
    }

    // View models refill Results with Clear + one Add per member, so a single keystroke raised
    // ~30 CollectionChanged events; rebuilding the rows on each one froze the UI for seconds
    // (an ANR on the day edit screen). The burst is coalesced into one refresh, posted to run
    // after the view model's synchronous refill has finished.
    private void OnResultsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_refreshPending) return;

        _refreshPending = true;
        Dispatcher.Dispatch(() =>
        {
            _refreshPending = false;
            RefreshVisibleResults();
        });
    }

    private bool _refreshPending;

    private void RefreshVisibleResults()
    {
        var all = Results?.Cast<object>().ToList() ?? [];
        var visible = all.Take(MaxVisibleResults).ToList();

        // Recreating the rows is the expensive part; skip it when the visible slice is unchanged
        // (e.g. a keystroke that only trims names beyond the cap).
        if (!visible.SequenceEqual(VisibleResults))
        {
            VisibleResults.Clear();
            foreach (var item in visible)
            {
                VisibleResults.Add(item);
            }
        }

        HasOverflow = all.Count > MaxVisibleResults;
        OverflowText = $"Mostrando {MaxVisibleResults} de {all.Count} — digite para filtrar";
        OnPropertyChanged(nameof(HasOverflow));
        OnPropertyChanged(nameof(OverflowText));
    }
}
