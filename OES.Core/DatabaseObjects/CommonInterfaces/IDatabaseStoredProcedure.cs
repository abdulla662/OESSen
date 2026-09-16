namespace OES.Core.DatabaseObjects.CommonInterfaces
{
    public interface IDatabaseStoredProcedure
    {
        string DropCommand { get; }

        string CreateCommand { get; }
    }
}
