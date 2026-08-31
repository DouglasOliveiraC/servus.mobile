using Servus.Mobile.Features.Admin;
using Servus.Mobile.Features.CalendarOverrides;
using Servus.Mobile.Features.Home;
using Servus.Mobile.Features.JoinOrganization;
using Servus.Mobile.Features.Members;
using Servus.Mobile.Features.Profile;
using Servus.Mobile.Features.Register;
using Servus.Mobile.Features.Schedule;
using Servus.Mobile.Features.Swaps;
using Servus.Mobile.Features.Unavailability;

namespace Servus.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));

		Routing.RegisterRoute(nameof(DayDetailPage), typeof(DayDetailPage));
		Routing.RegisterRoute(nameof(DayScheduleEditPage), typeof(DayScheduleEditPage));

		Routing.RegisterRoute(nameof(SwapRequestsPage), typeof(SwapRequestsPage));
		Routing.RegisterRoute(nameof(SwapHistoryPage), typeof(SwapHistoryPage));
		Routing.RegisterRoute(nameof(UnavailabilityPage), typeof(UnavailabilityPage));
		Routing.RegisterRoute(nameof(RosterSchedulePage), typeof(RosterSchedulePage));
		Routing.RegisterRoute(nameof(JoinOrganizationPage), typeof(JoinOrganizationPage));
		Routing.RegisterRoute(nameof(CalendarOverridesPage), typeof(CalendarOverridesPage));
		Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));

		Routing.RegisterRoute(nameof(OrganizationListPage), typeof(OrganizationListPage));
		Routing.RegisterRoute(nameof(RosterListPage), typeof(RosterListPage));
		Routing.RegisterRoute(nameof(RosterDetailPage), typeof(RosterDetailPage));
		Routing.RegisterRoute(nameof(RosterSettingsPage), typeof(RosterSettingsPage));
		Routing.RegisterRoute(nameof(MembersPage), typeof(MembersPage));
		Routing.RegisterRoute(nameof(MemberDetailPage), typeof(MemberDetailPage));
	}
}
