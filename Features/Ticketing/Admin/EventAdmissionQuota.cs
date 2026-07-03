namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Read and write for the one pricing field the Anlässe admin edits inline: the event
/// admission quota (Einlasskontingent). Everything else about pricing stays in its own UI; this keeps
/// the Anlässe table able to show and set the cap without pulling in the full price-set editor.</summary>
public interface IEventAdmissionQuota
{
    /// <summary>Admission quota per event id. An event without a price set, or with an unset quota, is
    /// absent from the map (treated as unlimited).</summary>
    Task<IReadOnlyDictionary<int, int?>> GetAllAsync();

    /// <summary>Sets the admission quota for an event (null = unlimited), preserving the rest of the
    /// event's price set. Creates a price set if none exists yet.</summary>
    Task SetAsync(int eventId, int? admissionQuota);
}
