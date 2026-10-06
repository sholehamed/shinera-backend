using Modules.System.Identity.Application.Features.Dashboard;
using Modules.System.Identity.Application.Authorization;
using Web.SharedKernel.Authorization;

namespace Modules.System.Identity.Web.Endpoints
{
    public class Dashboard : EndpointGroupBase
    {
        public override void Map(WebApplication app)
        {
            app.MapGroup(this)
                .MapGet(GetSuperDashboard, "super-dashboard", configure: endpoint => endpoint.RequirePermission(SystemPermissionCatalog.Dashboard.Resource, SystemPermissionCatalog.Dashboard.View));
        }

        public async Task<Ok<SuperDashboardDto>> GetSuperDashboard(IDispatcher sender, CancellationToken cancellationToken)
        {
            var res = await sender.Query(new SuperDashboardQuery(), cancellationToken);
            return TypedResults.Ok(res);
        }

    }
}
