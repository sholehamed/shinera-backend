using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Features.Users;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Users.Commands
{
    public record UserDeleteCommand(Guid Id) : ICommand;
    public class UserDeleteCommandHandler(IIdentityDbContext context) : ICommandHandler<UserDeleteCommand>
    {
        public async Task Handle(UserDeleteCommand command, CancellationToken cancellationToken)
        {
            User? entity = await context.CurrentTenantUsers().FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            Guard.Against.NotFound(command.Id, entity);
            // User is a global identity. Deletion must not cascade across workspaces.
            // The legacy delete command deactivates the account until a dedicated
            // anonymization/deletion workflow is introduced.
            entity.IsActive = false;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
