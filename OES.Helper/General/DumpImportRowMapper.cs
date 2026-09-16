using OES.Helper.Interfaces;
using System.Globalization;
using System.Reflection;

namespace OES.Helper.General
{
    public class DumpImportRowMapper : IRowMapper
    {
        public T MapRowToDto<T>(string[] headers, string[] values) where T : new()
        {
            var dto = new T();

            var standardProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < headers.Length; i++)
            {
                if (i >= values.Length) continue;

                var header = headers[i].Trim();

                var value = values[i].Trim();

                var normalizedHeader = header.Replace(" ", "");

                var property = standardProperties.FirstOrDefault(p => p.Name.Equals(normalizedHeader, StringComparison.OrdinalIgnoreCase));

                if (property != null && property.CanWrite)
                {
                    Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        if (Nullable.GetUnderlyingType(property.PropertyType) != null || !property.PropertyType.IsValueType)
                        {
                            property.SetValue(dto, null);
                        }

                        continue;
                    }
                    if (targetType == typeof(DateTime))
                    {
                        DateTime dateValue;

                        if (DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue) ||
                            DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue) ||
                            DateTime.TryParseExact(value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue) ||
                            DateTime.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue) ||
                            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                        {
                            property.SetValue(dto, dateValue);
                        }
                        else
                        {
                            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
                            {
                                property.SetValue(dto, null);
                            }
                        }
                    }
                    else
                    {
                        var convertedValue = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                        property.SetValue(dto, convertedValue);
                    }
                }
            }

            return dto;
        }
    }
}