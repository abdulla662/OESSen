using OES.Helper.Enums;

namespace OES.Helper.Dtos.Paper.Requests
{
    public sealed record UpdatePaperCreationStatusRequestDto(long PaperId, PaperCreationStatus PaperCreationStatus);
}
