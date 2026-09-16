using OES.Helper.Interfaces;

namespace OES.Helper.General
{
    public class RowMapper : IRowMapper
    {
        public T MapRowToDto<T>(string[] headers, string[] values) where T : new()
        {
            var dto = new T();

            var properties = typeof(T).GetProperties();

            for (int i = 0; i < headers.Length; i++)
            {
                var property = properties.FirstOrDefault(p => p.Name.ToLower() == headers[i].ToLower());

                if (property != null && i < values.Length)
                {
                    var value = values[i];

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        var convertedValue = Convert.ChangeType(value, targetType);

                        property.SetValue(dto, convertedValue);
                    }
                }
            }

            return dto;
        }
    }
}