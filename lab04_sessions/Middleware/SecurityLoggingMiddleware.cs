using Lab5.Filters;
using Lab5.Models;
using Lab5.Services;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Lab5.Middleware;

public class SecurityLoggingMiddleware
{
    public const string EventKey = "Lab6.SecurityEvent";

    private readonly RequestDelegate _next;

    public SecurityLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        SecurityLog log)
    {
        var action = context.GetEndpoint()?
            .Metadata.GetMetadata<ControllerActionDescriptor>();

        if (action == null)
        {
            await _next(context);
            return;
        }

        var failed = false;

        try
        {
            await _next(context);
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            var user = context.Items[
                PermissionFilter.CurrentUserKey] as AccessUser;

            var eventName = context.Items[EventKey] as string;

            eventName ??= failed
                ? "request.failed"
                : "request.completed";

            int? targetId = null;

            if (action.ControllerName == "Accounts" &&
                action.ActionName == "Edit" &&
                int.TryParse(
                    context.Request.RouteValues["id"]?.ToString(),
                    out var id))
            {
                targetId = id;
            }

            await log.WriteAsync(
                context,
                eventName,
                $"{action.ControllerName}.{action.ActionName}",
                user,
                targetId,
                failed ? 500 : context.Response.StatusCode);
        }
    }
}