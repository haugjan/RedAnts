using RedAnts.Show.Features.Board;

namespace RedAnts.Show.Features.Admin;

public static class ExportShowBoards
{
    public sealed record Query;

    public sealed class Handler(IShowProfiles profiles, IShowBoardPdf pdf)
    {
        public async Task<byte[]> HandleAsync(Query query)
        {
            var all = await profiles.GetAllAsync();
            return pdf.Render(all);
        }
    }
}
