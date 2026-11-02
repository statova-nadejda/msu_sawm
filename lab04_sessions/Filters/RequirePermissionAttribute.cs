using Lab5.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab5.Filters;

public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(typeof(PermissionFilter))
    {
        Arguments = new object[] { permission };
    }
}

public class PermissionFilter : IAsyncActionFilter
{
    public const string CurrentUserKey = "Lab5.CurrentUser";

    private readonly string _permission;
    private readonly AccessStore _store;

    public PermissionFilter(
        string permission,
        AccessStore store)
    {
        _permission = permission;
        _store = store;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        context.HttpContext.Response.Headers["Cache-Control"] =
            "no-store";

        var user = await _store.GetCurrentUserAsync(
            context.HttpContext);

        if (user == null)
        {
            await _store.EndSessionAsync(context.HttpContext);

            context.Result = new RedirectToActionResult(
                "Login",
                "Auth",
                null);

            return;
        }

        var allowed = await _store.HasPermissionAsync(
            user.Role,
            _permission);

        if (!allowed)
        {
            context.Result = new StatusCodeResult(
                StatusCodes.Status403Forbidden);

            return;
        }

        context.HttpContext.Items[CurrentUserKey] = user;

        await next();
    }
}