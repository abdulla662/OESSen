using Microsoft.AspNetCore.Components;
using OES.Helper.General;

namespace OES.Blazor.Components.Common
{
    public partial class GenericLookUp<T> : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "LookUp";
        [Parameter] public T Item { get; set; }
        [Parameter] public EventCallback<T> OnSave { get; set; }
        private T item;
        protected override void OnInitialized()
        {
            item = Item;
        }

        private List<LookUpData> GetProperties(object item)
        {
            var propertyList = new List<LookUpData>();

            if (item != null)
            {
                var properties = item.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item)?.ToString();
                    propertyList.Add(new LookUpData { Name = prop.Name, Description = value });
                }
            }

            return propertyList;
        }
        private async Task SaveData()
        {
            var properties = GetProperties(item);
            foreach (var property in properties)
            {
                var propInfo = item.GetType().GetProperty(property.Name);
                if (propInfo != null && propInfo.CanWrite)
                {
                    propInfo.SetValue(item, property.Description);
                }
            }

            await OnSave.InvokeAsync(item);
        }
    }
}
