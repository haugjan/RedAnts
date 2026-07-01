using RedAnts.Features.Ticketing.Ports;
using Sqids;

namespace RedAnts.Infrastructure.Shared;

/// <summary>Sqids-based encoder for short, opaque public ids (used for direct links to Intern events/seasons).</summary>
public sealed class SqidEncoder : ISqidEncoder
{
    // Min length keeps generated ids opaque and non-sequential-looking. Default Sqids alphabet
    // (guaranteed unique chars) is sufficient: this is obscurity for Intern links, not security.
    private readonly SqidsEncoder<int> _sqids = new(new SqidsOptions
    {
        MinLength = 8
    });

    public string Encode(int id) => _sqids.Encode(id);

    public int? Decode(string sqid)
    {
        if (string.IsNullOrWhiteSpace(sqid)) return null;
        var decoded = _sqids.Decode(sqid);
        return decoded.Count == 1 ? decoded[0] : null;
    }
}
