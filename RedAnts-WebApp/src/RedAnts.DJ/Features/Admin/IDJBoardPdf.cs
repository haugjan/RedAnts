using RedAnts.DJ.Domain;

namespace RedAnts.DJ.Features.Admin;

public interface IDJBoardPdf
{
    byte[] Render(IReadOnlyList<DJProfile> profiles);
}
