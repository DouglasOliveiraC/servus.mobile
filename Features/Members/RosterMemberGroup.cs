using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;

namespace Servus.Mobile.Features.Members;

// Rank é a posição relativa 1-based do membro dentro da fila deste roster (não o
// Position bruto do backend, que é um contador interno que só cresce a cada geração de
// escala e nunca reflete a ordem atual sozinho).
public record MemberGroupEntry(MemberDto Member, int Rank);

// Herda de ObservableCollection para ser aceito como grupo pelo CollectionView
// (IsGrouped exige que cada grupo também seja a própria lista de itens). Guarda todas
// as entradas originais em _allEntries e usa Clear/Add nesta coleção para exibir só o
// subconjunto filtrado — assim o grupo continua reconhecido pelo binding de grupo do
// CollectionView, sem precisar trocar a instância inteira a cada busca.
//
// IsExpanded é implementado manualmente (não com [ObservableProperty]) porque o source
// generator do MVVM Toolkit exige que a classe implemente INotifyPropertyChanged
// publicamente; ObservableCollection<T> já implementa a interface, mas com o evento
// PropertyChanged protegido, então a notificação usa o OnPropertyChanged herdado.
public class RosterMemberGroup : ObservableCollection<MemberGroupEntry>
{
    private readonly List<MemberGroupEntry> _allEntries;
    private bool _isExpanded;

    public string OrganizationName { get; }

    public string RosterName { get; }

    public int TotalCount => _allEntries.Count;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public ICommand ToggleExpandedCommand { get; }

    public RosterMemberGroup(string organizationName, string rosterName, IEnumerable<MemberGroupEntry> entries)
        : base(entries)
    {
        OrganizationName = organizationName;
        RosterName = rosterName;
        _allEntries = [.. entries];
        ToggleExpandedCommand = new Command(() => IsExpanded = !IsExpanded);
    }

    // Chamado a cada mudança na busca; expande automaticamente quando há um termo
    // buscado (para o resultado ficar visível sem exigir um toque extra) e recolhe
    // de volta quando a busca é limpa.
    public bool ApplyFilter(string normalizedSearch)
    {
        var filtered = normalizedSearch.Length == 0
            ? _allEntries
            : _allEntries.Where(e => e.Member.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();

        Clear();
        foreach (var entry in filtered)
        {
            Add(entry);
        }

        IsExpanded = normalizedSearch.Length > 0 && filtered.Count > 0;

        return filtered.Count > 0;
    }
}
