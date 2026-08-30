using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Members;
using Servus.Mobile.Services.SwapHistory;

namespace Servus.Mobile.Features.Swaps;

public record SwapHistoryEntryViewData(DateOnly Date, string FromMemberName, string ToMemberName, SwapCauseDto Cause);

[QueryProperty(nameof(SubrosterId), "subrosterId")]
public partial class SwapHistoryViewModel : ObservableObject
{
    private readonly ISwapHistoryService _swapHistoryService;
    private readonly IMemberService _memberService;

    public SwapHistoryViewModel(ISwapHistoryService swapHistoryService, IMemberService memberService)
    {
        _swapHistoryService = swapHistoryService;
        _memberService = memberService;
    }

    [ObservableProperty]
    private Guid subrosterId;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    public ObservableCollection<SwapHistoryEntryViewData> Entries { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || SubrosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var members = await _memberService.SearchMembersAsync(null);
            var namesById = members.ToDictionary(m => m.Id, m => m.Name);

            var history = await _swapHistoryService.GetBySubrosterIdAsync(SubrosterId);

            Entries.Clear();
            foreach (var entry in history.OrderByDescending(e => e.Date))
            {
                var fromName = namesById.GetValueOrDefault(entry.FromMemberId, entry.FromMemberId.ToString()[..8]);
                var toName = namesById.GetValueOrDefault(entry.ToMemberId, entry.ToMemberId.ToString()[..8]);
                Entries.Add(new SwapHistoryEntryViewData(entry.Date, fromName, toName, entry.Cause));
            }
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
