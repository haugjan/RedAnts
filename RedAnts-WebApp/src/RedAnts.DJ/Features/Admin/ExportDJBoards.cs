using RedAnts.DJ.Features.Board;

namespace RedAnts.DJ.Features.Admin;

public static class ExportDJBoards
{
    public sealed record Query;

    public sealed class Handler(IDJProfiles profiles, IDJBoardPdf pdf)
    {
        public async Task<byte[]> HandleAsync(Query query)
        {
            var all = await profiles.GetAllAsync();
            return pdf.Render(all);
        }
    }
}
