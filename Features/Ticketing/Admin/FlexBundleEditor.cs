namespace RedAnts.Features.Ticketing.Admin;

public interface IFlexBundleEditor
{
    Task RenameAsync(int bundleId, int seasonId, string newReference);
}
