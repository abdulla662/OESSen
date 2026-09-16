namespace OES.Core.DatabaseObjects.CommonInterfaces
{
    public interface IDatabaseFunction
    {
        string DropCommand { get; }

        string CreateCommand { get; }
    }
}
