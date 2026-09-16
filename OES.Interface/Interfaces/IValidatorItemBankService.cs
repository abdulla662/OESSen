using OES.Helper.Dtos.ItemBank;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IValidatorItemBankService
    {
        Task<ApiResponse> ValidateItemBankToEdit(EditItemBankNodeDto dto);
    }
}