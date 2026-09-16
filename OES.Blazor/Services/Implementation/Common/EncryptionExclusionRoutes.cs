using SharedHelper.General;

namespace OES.Blazor.Services.Implementation.Common
{
    public static class EncryptionExclusionRoutes
    {
        private static readonly HashSet<string> ExcludedRoutes =
        [
            $"{CentralizedUrlHelper.OesApiBaseUrl}api/UserProfile/LogIn",
            $"{CentralizedUrlHelper.OesApiBaseUrl}api/Candidate/ExportVerificationCodeExcel",
            $"{CentralizedUrlHelper.OesApiBaseUrl}api/AIAsset/GetAIAsset",
        ];

        public static bool IsExcluded(string url) => ExcludedRoutes.Contains(url);
    }
}
