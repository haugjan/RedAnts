
namespace RedAnts.Ticketing.Features.FlexTickets;

public static class RenameFlexBundle
{
    public sealed record Command(int BundleId, string Reference);

    public sealed class Handler(IFlexTicketBundles bundles)
    {
        public async Task HandleAsync(Command command)
        {
            var bundle = await bundles.GetByIdAsync(command.BundleId)
                ?? throw new DomainException("Bundle wurde nicht gefunden.");
            var reference = FlexBundleReference.Clean(command.Reference);
            if (reference == bundle.Reference) return;
            await FlexBundleReference.RequireFreeAsync(bundles, bundle.SeasonId, reference);
            bundle.Rename(reference);
            await bundles.SaveAsync(bundle);
        }
    }
}
