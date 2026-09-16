
namespace OES.Helper.Dtos.Paper.Requests
{
    public sealed record PaperLockRequestDto(
        long PaperId,
        string SessionId
    );
}
