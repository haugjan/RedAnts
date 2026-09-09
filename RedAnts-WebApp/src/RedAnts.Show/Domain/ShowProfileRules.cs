namespace RedAnts.Show.Domain;

public static class ShowProfileRules
{
    public const string BlankId = "Jedes Profil braucht eine Id.";
    public const string BlankName = "Jedes Profil braucht einen Namen.";
    public const string DuplicateId = "Profil-Ids müssen eindeutig sein: {0}";

    public static void Validate(IReadOnlyList<ShowProfile> profiles)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id)) throw new DomainException(BlankId);
            if (string.IsNullOrWhiteSpace(profile.Name)) throw new DomainException(BlankName);
            if (!seen.Add(profile.Id.Trim())) throw new DomainException(string.Format(DuplicateId, profile.Id.Trim()));
        }
    }
}
