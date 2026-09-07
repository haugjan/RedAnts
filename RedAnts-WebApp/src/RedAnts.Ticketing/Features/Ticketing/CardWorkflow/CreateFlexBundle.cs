using RedAnts.Domain.Ticketing.Sales;
using RedAnts.Features.Ticketing.Ports;

namespace RedAnts.Features.Ticketing.CardWorkflow;

public static class CreateFlexBundle
{
    public sealed record Command(int SeasonId, TicketCategory Category, string Reference, int Quantity,
        string? CreatedByName, string? CreatedByEmail, int? OrderId);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public async Task<FlexTicketBundleView> HandleAsync(Command command)
        {
            var reference = FlexBundleReference.Clean(command.Reference);
            if (command.Quantity < 1) throw new DomainException("Menge muss mindestens 1 sein.");
            await FlexBundleReference.RequireFreeAsync(bundles, command.SeasonId, reference);
            return await bundles.CreateAsync(command.SeasonId, command.Category, reference, command.Quantity,
                command.CreatedByName, command.CreatedByEmail, command.OrderId);
        }
    }
}

internal static class FlexBundleReference
{
    public static string Clean(string? reference)
    {
        var value = (reference ?? "").Trim();
        if (value.Length == 0) throw new DomainException("Bitte ein Bundle angeben.");
        if (value.Length > FlexTicketBundle.ReferenceMaxLength)
            throw new DomainException($"Das Bundle darf höchstens {FlexTicketBundle.ReferenceMaxLength} Zeichen lang sein.");
        return value;
    }

    public static async Task RequireFreeAsync(IFlexTicketBundles bundles, int seasonId, string reference)
    {
        if (await bundles.ReferenceExistsAsync(seasonId, reference))
            throw new DomainException($"Das Bundle „{reference}“ ist in dieser Saison bereits vergeben.");
    }
}
