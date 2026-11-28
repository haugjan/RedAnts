using Microsoft.AspNetCore.Components;
using RedAnts.Show.Domain;
using RedAnts.Show.Features.TabBoards;

namespace RedAnts.Show.Features.Admin;

public abstract class TabBoardTab : ComponentBase
{
    [Inject] protected GetTabBoard.Handler BoardQuery { get; set; } = default!;
    [Inject] protected SaveTabBoard.Handler BoardSave { get; set; } = default!;

    protected TabBoard Board { get; private set; } = TabBoard.Empty;

    protected string? Status { get; private set; }

    protected bool StatusIsError { get; private set; }

    protected bool Dirty { get; set; }

    protected override async Task OnInitializedAsync() => await LoadAsync();

    protected virtual Task OnBoardLoadedAsync() => Task.CompletedTask;

    protected async Task LoadAsync()
    {
        Board = await BoardQuery.HandleAsync(new GetTabBoard.Query());
        Dirty = false;
        await OnBoardLoadedAsync();
    }

    protected void Touch() => Dirty = true;

    protected async Task<bool> SaveBoardAsync(Func<TabBoard, TabBoard> change)
    {
        Status = null;
        try
        {
            Board = await BoardSave.HandleAsync(new SaveTabBoard.Command(change(Board)));
            Dirty = false;
            await OnBoardLoadedAsync();
            Notify("Gespeichert.");
            return true;
        }
        catch (DomainException ex)
        {
            Fail(ex.Message);
            return false;
        }
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
