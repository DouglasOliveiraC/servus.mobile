namespace Servus.Mobile.Services.Navigation;

public static class Routes
{
    public const string Login = nameof(Features.Login.LoginPage);
    public const string Register = nameof(Features.Register.RegisterPage);

    // AppShell.xaml registrava essa rota como "MainPage" (não batia com
    // nameof(DashboardPage)) — corrigido junto com esta centralização.
    public const string Dashboard = nameof(Features.Home.DashboardPage);

    public const string DayDetail = nameof(Features.Home.DayDetailPage);
    public const string DayScheduleEdit = nameof(Features.Home.DayScheduleEditPage);
    public const string SwapRequests = nameof(Features.Swaps.SwapRequestsPage);
    public const string SwapHistory = nameof(Features.Swaps.SwapHistoryPage);
    public const string Unavailability = nameof(Features.Unavailability.UnavailabilityPage);
    public const string RosterSchedule = nameof(Features.Schedule.RosterSchedulePage);
    public const string JoinOrganization = nameof(Features.JoinOrganization.JoinOrganizationPage);
    public const string CalendarOverrides = nameof(Features.CalendarOverrides.CalendarOverridesPage);
    public const string Profile = nameof(Features.Profile.ProfilePage);
    public const string OrganizationList = nameof(Features.Admin.OrganizationListPage);
    public const string RosterList = nameof(Features.Admin.RosterListPage);
    public const string RosterDetail = nameof(Features.Admin.RosterDetailPage);
    public const string RosterSettings = nameof(Features.Admin.RosterSettingsPage);
    public const string Members = nameof(Features.Members.MembersPage);
    public const string MemberDetail = nameof(Features.Members.MemberDetailPage);
}
