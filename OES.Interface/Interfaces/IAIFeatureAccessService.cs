namespace OES.Interface.Interfaces
{
    public interface IAIFeatureAccessService
    {
        Task<bool> HasAIFeaturesAccessAsync(long organizationId);
    }
}
