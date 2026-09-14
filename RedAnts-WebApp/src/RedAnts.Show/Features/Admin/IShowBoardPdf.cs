using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public interface IShowBoardPdf
{
    byte[] Render(IReadOnlyList<ShowProfile> profiles);
}
