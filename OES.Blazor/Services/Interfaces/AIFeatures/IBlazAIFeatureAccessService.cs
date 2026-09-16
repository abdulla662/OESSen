namespace OES.Blazor.Services.Interfaces.AIFeatures
{
    public interface IBlazAIFeatureAccessService
    {
        Task<bool> HasAIFeaturesAccessAsync(long organizationId);
    }
}
