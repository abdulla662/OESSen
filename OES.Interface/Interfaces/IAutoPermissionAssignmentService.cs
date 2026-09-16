using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.AutoPermissionAssignment.Response;

namespace OES.Interface.Interfaces
{
    public interface IAutoPermissionAssignmentService
    {
        Task<AutoPermissionAssignmentResponse> AssignDefaultPermissionsForNewEntityAsync(AutoPermissionAssignmentRequest request);
    }
}
