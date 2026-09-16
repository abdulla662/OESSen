using OES.Helper.Dtos.ILO;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IILOValidatorService
    {
        Task<ApiResponse> ValidateNewRoot(ILONewRootDto dto);
        Task<ApiResponse> ValidateEditRoot(IloEditDTO dto);
    }
}
