
namespace OES.Helper.Dtos.Paper.Responses
{
    public sealed record AcquirePaperLockResponseDto(
        string SessionId,
        DateTime LockExpiresAt
    );
}
