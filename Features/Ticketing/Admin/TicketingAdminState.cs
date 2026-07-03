namespace RedAnts.Features.Ticketing.Admin;

/// <summary>
/// Per-circuit shared state for the Blazor ticketing admin app. Blazor Server scopes this to the
/// SignalR connection, so every admin tab component shares one instance for the lifetime of the
/// open admin session. It currently just remembers the season the admin last picked, so switching
/// tabs keeps the selection instead of each tab resetting to the current season.
///
/// Contract for tab components: on init, use <see cref="SelectedSeasonId"/> when it is set and still
/// valid, otherwise fall back to the tab's own default (the current season) and write that back here;
/// whenever the season dropdown changes, write the new value back. A fresh circuit (admin app reload)
/// starts with a null selection, so the first tab shown re-establishes the current-season default.
/// </summary>
public sealed class TicketingAdminState
{
    /// <summary>The season id the admin last selected, or null before any tab has set one.</summary>
    public int? SelectedSeasonId { get; set; }
}
