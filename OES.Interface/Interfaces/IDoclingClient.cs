namespace OES.Interface.Interfaces
{
    public interface IDoclingClient
    {
        Task<string> ConvertAsync(Stream file, string fileName, bool needOcr = true, CancellationToken cancellationToken = default);
    }
}
