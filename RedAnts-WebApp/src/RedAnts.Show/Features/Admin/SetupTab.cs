using Microsoft.AspNetCore.Components;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.Setup;

namespace RedAnts.Show.Features.Admin;

public abstract class SetupTab : ComponentBase
{
    [Inject] protected GetShowSetup.Handler SetupQuery { get; set; } = default!;
    [Inject] protected SaveShowSetup.Handler SetupSave { get; set; } = default!;

    protected ShowSetup Setup { get; private set; } = ShowSetup.Empty;

    protected string? Status { get; private set; }

    protected bool StatusIsError { get; private set; }

    protected bool Dirty { get; set; }

    protected override async Task OnInitializedAsync() => await LoadAsync();

    protected virtual Task OnSetupLoadedAsync() => Task.CompletedTask;

    protected async Task LoadAsync()
    {
        Setup = await SetupQuery.HandleAsync(new GetShowSetup.Query());
        Dirty = false;
        await OnSetupLoadedAsync();
    }

    protected void Touch() => Dirty = true;

    protected async Task SaveSetupAsync(Func<ShowSetup, ShowSetup> change) => await Guarded(async () =>
    {
        Setup = await SetupSave.HandleAsync(new SaveShowSetup.Command(change(Setup)));
        Dirty = false;
        await OnSetupLoadedAsync();
        Notify("Gespeichert; verbundene Agenten haben die Änderung bereits.");
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

    protected void Fail(string text)
    {
        Status = text;
        StatusIsError = true;
    }
}
