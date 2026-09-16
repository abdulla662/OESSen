using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace OES.Blazor.Extensions
{
    public class CustomComponentActivator : IComponentActivator
    {
        public IComponent CreateInstance(Type componentType)
        {
            var instance = (IComponent)Activator.CreateInstance(componentType)!;

            if (componentType.IsGenericType && componentType.GetGenericTypeDefinition() == typeof(MudAutocomplete<>))
            {
                PropertyInfo propertyInfo = componentType.GetProperty(nameof(MudAutocomplete<object>.MaxItems));

                propertyInfo?.SetValue(instance, null);
            }

            return instance;
        }
    }
}
