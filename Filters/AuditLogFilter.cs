using Microsoft.AspNetCore.Mvc.Filters;

namespace TmsApi.Filters;

public class AuditLogFilter(
    ILogger<AuditLogFilter> logger)
    : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        logger.LogInformation(
            "TMS API call: {Method} {Route}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        logger.LogInformation(
            "TMS API response: {StatusCode}",
            context.HttpContext.Response.StatusCode);
    }
}