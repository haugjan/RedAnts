namespace RedAnts.Features.Ticketing.Admin;

public sealed class TicketingAdminState
{
    private int? _seasonId;
    private int? _eventId;
    private int? _bundleId;

    public event Action? Changed;

    public int? SelectedSeasonId { get => _seasonId; set => Set(ref _seasonId, value); }
    public int? SelectedEventId { get => _eventId; set => Set(ref _eventId, value); }
    public int? SelectedBundleId { get => _bundleId; set => Set(ref _bundleId, value); }

    private void Set(ref int? field, int? value)
    {
        if (field == value) return;
        field = value;
        Changed?.Invoke();
    }
}
