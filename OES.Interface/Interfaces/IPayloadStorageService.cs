namespace OES.Interface.Interfaces
{
    public interface IPayloadStorageService
    {
        Task<string> WritePayloadAsync(string fileName, string jsonContent);

        Task<string> ReadPayloadAsync(string fileName);

        Task DeletePayloadAsync(string fileName);

        Task<Stream> GetPayloadStreamAsync(string fileName);
    }
}