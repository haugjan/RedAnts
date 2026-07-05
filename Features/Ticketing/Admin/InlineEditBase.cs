using Microsoft.AspNetCore.Components;

namespace RedAnts.Features.Ticketing.Admin;

public abstract class InlineEditBase<TValue> : ComponentBase
{
    [Parameter] public TValue Value { get; set; } = default!;
    [Parameter] public string FieldLabel { get; set; } = "Wert";
    [Parameter] public EventCallback<TValue> Save { get; set; }
    [Parameter] public bool Disabled { get; set; }

    protected bool Editing;
    protected bool ConfirmOpen;
    protected bool Busy;
    protected string? Error;
    protected TValue Pending = default!;

    protected abstract string FormatValue(TValue value);

    protected string ConfirmTitle => $"{FieldLabel} ändern";

    protected string ConfirmMessage =>
        $"{FieldLabel} von „{FormatValue(Value)}“ auf „{FormatValue(Pending)}“ ändern?";

    protected void BeginEdit()
    {
        if (Disabled) return;
        Pending = Value;
        Error = null;
        Editing = true;
    }

    protected void CancelEdit()
    {
        Editing = false;
        ConfirmOpen = false;
        Error = null;
    }

    protected void RequestSave(TValue newValue)
    {
        Pending = newValue;
        if (EqualityComparer<TValue>.Default.Equals(newValue, Value) ||
            FormatValue(newValue) == FormatValue(Value))
        {
            CancelEdit();
            return;
        }
        Error = null;
        ConfirmOpen = true;
    }

    protected async Task ConfirmSaveAsync()
    {
        Busy = true;
        Error = null;
        try
        {
            await Save.InvokeAsync(Pending);
            ConfirmOpen = false;
            Editing = false;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Busy = false;
        }
    }

    protected void DismissConfirm()
    {
        if (Busy) return;
        ConfirmOpen = false;
        Editing = false;
        Error = null;
    }
}
