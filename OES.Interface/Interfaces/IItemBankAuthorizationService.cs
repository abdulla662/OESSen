using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IItemBankAuthorizationService
    {
        Task<bool> CanUseItemBankAsync(long itemBankId);

        Task<ApiResponse> CanDoQuestionActionAsync(long itemBankId, string requiredRole);
    }
}
