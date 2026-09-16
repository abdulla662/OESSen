namespace OES.Helper.Interfaces
{
    public interface IRowMapper
    {
        T MapRowToDto<T>(string[] headers, string[] values) where T : new();
    }
}
