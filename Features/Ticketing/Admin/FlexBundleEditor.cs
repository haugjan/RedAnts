namespace RedAnts.Features.Ticketing.Admin;

/// <summary>Write side for the Flextickets admin tab: renames a bundle's reference (the one sensibly
/// editable detail of an already-issued bundle; category and size are structural). Kept in the admin
/// slice so it does not widen the bundle creation port.</summary>
public interface IFlexBundleEditor
{
    /// <summary>Renames the bundle's reference. Throws if the new reference is empty or already used by
    /// another bundle in the same season.</summary>
    Task RenameAsync(int bundleId, int seasonId, string newReference);
}
