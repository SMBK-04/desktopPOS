using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using EnterprisePOS.Data;

namespace EnterprisePOS.Filters
{
    public class SubscriptionActiveFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // Allow access to Billing, Account controller (specifically Login and Logout)
            if (string.Equals(controller, "Billing", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                // SuperAdmin bypasses billing checks
                if (user.IsInRole("SuperAdmin"))
                {
                    await next();
                    return;
                }

                var tenantIdClaim = user.FindFirst("TenantId")?.Value;
                if (Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    
                    // Bypass EF Global Query Filter to find Tenant details
                    var tenant = await db.Tenants
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(t => t.Id == tenantId);

                    if (tenant != null)
                    {
                        // 1. If Tenant is BLOCKED by SuperAdmin: Strictly prevent access and redirect to blocked page
                        if (tenant.IsBlocked)
                        {
                            context.Result = new RedirectToRouteResult(new RouteValueDictionary
                            {
                                { "controller", "Billing" },
                                { "action", "PayYourBills" }
                            });
                            return;
                        }

                        // 2. If subscription date has arrived/passed: DO NOT close POS!
                        // Allow them to continue using the system, but flag it so warning modal and footer are shown.
                        var isExpired = TimeHelper.PKT > tenant.SubscriptionEndDate;
                        if (isExpired)
                        {
                            context.HttpContext.Items["IsSubscriptionExpired"] = true;
                            context.HttpContext.Items["SubscriptionEndDate"] = tenant.SubscriptionEndDate;

                            if (context.Controller is Controller mvcController)
                            {
                                mvcController.ViewBag.IsSubscriptionExpired = true;
                                mvcController.ViewBag.SubscriptionEndDate = tenant.SubscriptionEndDate;
                            }
                        }
                    }
                }
            }

            await next();
        }
    }
}
