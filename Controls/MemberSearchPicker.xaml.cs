using System.Collections;
using System.Windows.Input;

namespace Servus.Mobile.Controls;

// Reúne a estrutura duplicada em SubrosterDetailPage (busca de membro para "Adicionar"
// e para "Conceder acesso"): SfTextInputLayout de busca + CollectionView de resultados
// clicáveis. Cada tela mantém seu próprio ViewModel/estado de busca por trás — este
// controle só une o binding de texto, a lista e o comando de seleção num único bloco
// visual, para as duas telas não poderem divergir em highlight/estilo sem querer.
public partial class MemberSearchPicker : ContentView
{
    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(MemberSearchPicker), "Buscar membro");

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(MemberSearchPicker), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty ResultsProperty =
        BindableProperty.Create(nameof(Results), typeof(IEnumerable), typeof(MemberSearchPicker));

    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(MemberSearchPicker));

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
}
