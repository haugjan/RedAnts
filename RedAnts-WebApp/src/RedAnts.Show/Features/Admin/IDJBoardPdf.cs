using RedAnts.Show.Domain;

namespace RedAnts.Show.Features.Admin;

public interface IDJBoardPdf
{
    byte[] Render(IReadOnlyList<DJProfile> profiles);
}
