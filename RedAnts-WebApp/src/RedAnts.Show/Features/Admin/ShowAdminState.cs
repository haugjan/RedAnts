namespace RedAnts.Show.Features.Admin;

public sealed class ShowAdminState
{
    private string? _hallId;

    public event Action? Changed;

    public string? SelectedHallId
    {
        get => _hallId;
        set
        {
            if (_hallId == value) return;
            _hallId = value;
            Changed?.Invoke();
        }
    }
}
