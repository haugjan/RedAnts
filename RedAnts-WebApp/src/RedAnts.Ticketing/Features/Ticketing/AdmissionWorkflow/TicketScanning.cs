using RedAnts.Domain.Ticketing.Admission;
using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;
using RedAnts.Features.Ticketing.Tickets;

namespace RedAnts.Features.Ticketing.AdmissionWorkflow;

public sealed class TicketScanning(
    IAdmissionFactsReader facts,
    IAdmissionRepository admissions,
    ITicketRedemptions redemptions,
    IOccupancyReader occupancy)
{
    public async Task<ScanOutcome> ScanAsync(int eventId, TicketType type, Guid uuid, int scopeId, ScanMode mode, string? scannedBy, bool test)
    {
        if (uuid == Guid.Empty)
            return new ScanOutcome(AdmissionOutcome.Test, type, "TEST", null, await occupancy.GetAsync(eventId), CategoryLabel: "Scanner-Test");

        var known = await facts.ReadAsync(eventId, type, uuid);
        var admission = await admissions.LoadAsync(eventId, type, uuid);
        var isMember = type == TicketType.MemberCard;
        var cap = isMember ? Math.Max(1, known.Issued?.Admissions ?? 1) : 1;

        var evaluation = AdmissionEvaluator.Evaluate(new ScanContext(
            eventId, type, scopeId, mode, test, false,
            known.Issued is null ? null : new ScannedTicketFacts(known.Issued.Type, known.Issued.ScopeId, known.Issued.Status),
            known.EventSeasonId, known.RedeemedEventId, admission.InsideCount, cap, known.RequiresConversion, known.IsBoxOfficeFlex));

        var reference = Reference(uuid);
        var categoryLabel = known.Issued is null ? null : known.Issued.CategoryName ?? known.Issued.Category?.DisplayName();
        var holder = known.Issued is null ? null : HolderLabel(known.Issued);
        int? capOut = isMember ? cap : null;

        switch (evaluation.Verdict)
        {
            case AdmissionVerdict.TestEmpty:
                return new ScanOutcome(AdmissionOutcome.Test, type, "TEST", null, await occupancy.GetAsync(eventId), CategoryLabel: "Scanner-Test");

            case AdmissionVerdict.TestTicket:
                return new ScanOutcome(AdmissionOutcome.Test, type, reference, null, await occupancy.GetAsync(eventId), categoryLabel, holder);

            case AdmissionVerdict.Reject when evaluation.Reason is AdmissionEvaluator.AlreadyCheckedIn or AdmissionEvaluator.AllAdmissionsUsed:
                if (isMember)
                {
                    var priors = admission.CheckIns.Select(l => new PriorScan(l.OccurredAt, l.ScannedBy)).ToList();
                    return new ScanOutcome(AdmissionOutcome.Rejected, type, reference, evaluation.Reason,
                        await occupancy.GetAsync(eventId), categoryLabel, holder,
                        priors.LastOrDefault()?.At, priors.LastOrDefault()?.By, admission.InsideCount, capOut, priors);
                }
                var prior = admission.LastCheckIn;
                return new ScanOutcome(AdmissionOutcome.Rejected, type, reference, evaluation.Reason,
                    await occupancy.GetAsync(eventId), categoryLabel, holder, prior?.OccurredAt, prior?.ScannedBy);

            case AdmissionVerdict.Reject:
                var carries = AdmissionEvaluator.CarriesHolder(evaluation.Reason);
                return new ScanOutcome(AdmissionOutcome.Rejected, type, reference, evaluation.Reason,
                    await occupancy.GetAsync(eventId), carries ? categoryLabel : null, carries ? holder : null);
        }

        var now = DateTime.UtcNow;
        if (mode == ScanMode.CheckIn)
        {
            admission.CheckIn(cap, scannedBy, now, known.OriginType, known.OriginCardUuid);
            await admissions.SaveAsync(admission);
            if (type is TicketType.SeasonSingle or TicketType.EventTicket)
                await redemptions.MarkRedeemedAsync(type, uuid, eventId);
            return new ScanOutcome(AdmissionOutcome.CheckedIn, type, reference, null,
                await occupancy.GetAsync(eventId), categoryLabel, holder,
                AdmissionsUsed: isMember ? admission.InsideCount : null, AdmissionCap: capOut);
        }

        admission.CheckOut(scannedBy, now);
        await admissions.SaveAsync(admission);
        return new ScanOutcome(AdmissionOutcome.CheckedOut, type, reference, null,
            await occupancy.GetAsync(eventId), categoryLabel, holder,
            AdmissionsUsed: isMember ? admission.InsideCount : null, AdmissionCap: capOut);
    }

    private static string Reference(Guid uuid) => uuid.ToString("N")[..8].ToUpperInvariant();

    private static string? HolderLabel(IssuedTicket ticket)
    {
        if (ticket.Type == TicketType.MemberCard)
        {
            if (string.IsNullOrWhiteSpace(ticket.HolderName)) return null;
            return Age(ticket.Birthday) is { } age ? $"{ticket.HolderName} ({age})" : ticket.HolderName;
        }
        return string.IsNullOrWhiteSpace(ticket.BuyerName) ? null : ticket.BuyerName;
    }

    private static int? Age(DateOnly? birthday)
    {
        if (birthday is not { } born) return null;
        var today = SwissTime.Today;
        var age = today.Year - born.Year;
        if (born > today.AddYears(-age)) age--;
        return age is < 0 or > 120 ? null : age;
    }
}
