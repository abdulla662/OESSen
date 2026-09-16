using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;

namespace OES.API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AIFeaturesRequiredAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var aiFeatureAccessService = context
                .HttpContext
                .RequestServices
                .GetService<IAIFeatureAccessService>();

            var _filterParamsValues = (FilterParamsValues)context.HttpContext.RequestServices.GetService(typeof(FilterParamsValues));

            if (_filterParamsValues != null)
            {
                _filterParamsValues.OrganizationId = long.TryParse(
                    context.HttpContext.User?.Claims.FirstOrDefault(c => c.Type == CustomJwtClaimsTypes.OrganizationId)?.Value,
                    out var orgId
                ) ? orgId : 0L;
            }

            if (aiFeatureAccessService == null || _filterParamsValues == null)
            {
                context.Result = new ObjectResult(new
                {
                    message = Resource.AIFeaturesAccessServiceUnavailable,
                    success = false
                })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };

                return;
            }

            bool hasAccess = await aiFeatureAccessService.HasAIFeaturesAccessAsync(_filterParamsValues.OrganizationId);

            if (!hasAccess)
            {
                context.Result = new ObjectResult(new
                {
                    message = Resource.AIFeaturesAccessDenied,
                    success = false
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

                return;
            }

            await next();
        }
    }
}
