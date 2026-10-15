using Microsoft.AspNetCore.Components;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Halls;

namespace RedAnts.Show.Features.Admin;

public abstract class HallTab : ComponentBase, IDisposable
{
    [Inject] protected ShowAdminState State { get; set; } = default!;
    [Inject] protected GetHall.Handler HallQuery { get; set; } = default!;
    [Inject] protected SaveHall.Handler HallSave { get; set; } = default!;

    protected Hall? Hall { get; private set; }

    protected string? Status { get; private set; }

    protected bool StatusIsError { get; private set; }

    protected bool Dirty { get; set; }

    protected override async Task OnInitializedAsync()
    {
        State.Changed += OnHallSwitched;
        await LoadAsync();
    }

    protected virtual Task OnHallLoadedAsync() => Task.CompletedTask;

    private readonly SemaphoreSlim _loading = new(1, 1);

    protected async Task LoadAsync()
    {
        await _loading.WaitAsync();
        try
        {
            Hall = State.SelectedHallId is { Length: > 0 } id ? await HallQuery.HandleAsync(new GetHall.Query(id)) : null;
            Dirty = false;
            await OnHallLoadedAsync();
        }
        finally { _loading.Release(); }
    }

    protected void Touch() => Dirty = true;

    protected async Task SaveAsync(Func<Hall, Hall> change) => await Guarded(async () =>
    {
        if (Hall is null) return;
        Hall = await HallSave.HandleAsync(new SaveHall.Command(change(Hall)));
        Dirty = false;
        await OnHallLoadedAsync();
        Notify("Gespeichert; ein verbundener Agent hat die Änderung bereits.");
    });

    protected async Task Guarded(Func<Task> work)
    {
        Status = null;
        try { await work(); }
        catch (DomainException ex) { Status = ex.Message; StatusIsError = true; }
    }

    protected void Notify(string text)
    {
        Status = text;
        StatusIsError = false;
    }

    private async void OnHallSwitched()
    {
        await LoadAsync();
        await InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose() => State.Changed -= OnHallSwitched;
}
