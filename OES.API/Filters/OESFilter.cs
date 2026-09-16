using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Infrastructure.Contexts;
using OES.Interface.Interfaces;
using OES.Services.Services;
using SharedHelper.General;
using SharedHelper.RolesNames;
using System.Globalization;
namespace OES.API.Filters;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public class OESFilterAttribute : Attribute, IAsyncActionFilter
{
    public bool Authorize { get; set; }

    public bool ShowDeleted { get; set; }

    public List<UserRole> SsoUserRoles { get; set; } = [];

    public bool IsActive { get; set; } = true;

    public bool ApplyFilter { get; set; } = true;

    public bool ApplyShowDeletedFilter { get; set; }

    public bool ApplyIsActiveFilter { get; set; }

    public bool ApplyOrganizationIdFilter { get; set; } = true;

    public bool ApplySignatureFilter { get; set; } = true;

    public bool IsEncryptedQueryString { get; set; }

    public bool IsEncryptedBody { get; set; }



    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var servicesContainer = context.HttpContext.RequestServices;

        var _filterParamsValues = (FilterParamsValues)servicesContainer.GetService(typeof(FilterParamsValues));

        var _cacheService = (APIMemoryCach)servicesContainer.GetService(typeof(IAPIMemoryCach));

        var _currentPathRoles = await _cacheService.GetPathRoles(context.HttpContext.Request.Path.ToString().Substring(1), true);

        if (Authorize)
        {
            var ssoUserRolesStringified = context.HttpContext.User!.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.SsoUserRoles)?.Value;

            var ssoUserRoles = ssoUserRolesStringified is not null ? JsonConvert.DeserializeObject<List<UserRole>>(ssoUserRolesStringified) ?? ([]) : [];

            var userIdClaimValue = context.HttpContext.User!.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.ModuleUserID)?.Value;

            var userId = Guid.TryParse(userIdClaimValue, out Guid parsedUserId) ? parsedUserId : Guid.NewGuid();

            var response = await servicesContainer.GetRequiredService<IUserProfileService>().GetUserGroupsAndRolesAsync(userId);

            var userGroupsAndRolesList = (List<UserGroupsAndRolesDto>)response.Data;

            SsoUserRoles.Clear();

            ssoUserRoles.ForEach(ssoUserRole => SsoUserRoles.Add(ssoUserRole));


            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            else if (_currentPathRoles?.Length > 0)
            {
                var passed = false;

                if (ssoUserRoles.Exists(ur => ur.Name == AdminRoles.SuperAdmin))
                {
                    passed = true;
                    goto passingLabel;
                }

                if (ssoUserRoles.Exists(ur => ur.Name == AdminRoles.Entity_Admin))
                {
                    passed = true;
                    goto passingLabel;
                }

                var oesRoleNames = userGroupsAndRolesList?
                    .SelectMany(x => x.GroupRoles)
                    .Select(x => x.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

                if (Array.Exists(_currentPathRoles, pr =>
                    SsoUserRoles.Select(x => x.Name).Contains(pr) || oesRoleNames.Contains(pr)))
                {
                    passed = true;
                    goto passingLabel;
                }

            passingLabel:
                if (!passed)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            _filterParamsValues.OrganizationId = long.Parse(context!.HttpContext!.User!.Claims!.FirstOrDefault(x => x.Type == CustomJwtClaimsTypes.OrganizationId)!.Value);
            _filterParamsValues.Signature = context!.HttpContext!.User!.Claims!.FirstOrDefault(x => x.Type == CustomJwtClaimsTypes.OrganizationSignature)!.Value;
            _filterParamsValues.UserId = userId.ToString();
            _filterParamsValues.SsoUserRoles = SsoUserRoles;
            _filterParamsValues.ModuleId = Guid.Parse(context!.HttpContext.User!.Claims!.FirstOrDefault(x => x.Type == CustomJwtClaimsTypes.ModuleId)?.Value);
            _filterParamsValues.CurrentBearerToken = context.HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty);
            _filterParamsValues.OesUserGroupsAndRoles = userGroupsAndRolesList; // Should be revised as roles should be mixed.
            _filterParamsValues.UserEmail = context!.HttpContext.User!.Claims!.FirstOrDefault(x => x.Type == CustomJwtClaimsTypes.UserEmail)?.Value;
        }

        _filterParamsValues.ShowDeleted = ShowDeleted;
        _filterParamsValues.IsActive = IsActive;
        _filterParamsValues.ApplyFilter = ApplyFilter;
        _filterParamsValues.ApplySignatureFilter = ApplySignatureFilter;
        _filterParamsValues.ApplyIsActiveFilter = ApplyIsActiveFilter;
        _filterParamsValues.ApplyOrganizationIdFilter = ApplyOrganizationIdFilter;
        _filterParamsValues.Authorize = Authorize;

        var dbContext = context.HttpContext.RequestServices.GetService(typeof(AppDbContext)) as AppDbContext;

        dbContext.ApplyFilter(_filterParamsValues);

        await next();
    }

    private static object? ConvertToType(string value, Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);

        if (value.Length > 0)
        {
            if (type == typeof(DateTimeOffset) || underlyingType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            }

            if (type == typeof(DateTime) || underlyingType == typeof(DateTime))
            {
                return DateTime.Parse(value, CultureInfo.InvariantCulture);
            }

            if (type == typeof(Guid) || underlyingType == typeof(Guid))
            {
                return new Guid(value);
            }

            if (type == typeof(Uri) || underlyingType == typeof(Uri))
            {
                if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
                {
                    return uri;
                }

                return null;
            }
        }
        else
        {
            if (type == typeof(Guid))
            {
                return default(Guid);
            }

            if (underlyingType != null)
            {
                return null;
            }
        }

        if (underlyingType is not null)
        {
            return Convert.ChangeType(value, underlyingType);
        }

        return Convert.ChangeType(value, type);
    }
}
