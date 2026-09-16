using Newtonsoft.Json;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Common
{
    public class BlazGetCustomTableDataService<T> : IBlazGetCustomTableData<T> where T : class
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazGetCustomTableDataService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<CustomTableData<T>> GetCustomTableData(PaginationSearchModel paginationSearchModel, string apiUrl)
        {
            var ApiResponse = await _httpClientHelper.PostAsync(paginationSearchModel, apiUrl);

            CustomTableData<T> result = null;

            if (ApiResponse?.Data != null)
            {
                result = JsonConvert.DeserializeObject<CustomTableData<T>>(ApiResponse.Data.ToString());
            }
            else
            {
                // Handle null data scenario
                result = new CustomTableData<T>([], 0);
            }

            return result;
        }
    }
}
