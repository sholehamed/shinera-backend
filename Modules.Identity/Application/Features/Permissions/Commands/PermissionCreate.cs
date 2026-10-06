using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;

namespace Modules.System.Identity.Application.Features.Permissions.Commands
{
   
    public record PermissionCreateCommand : MapTo<Permission>, ICommand<Guid>
    {
        public string Code { get; set; }
        public Guid ResourceId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public ICollection<PermissionResource>? PermissionResources { get; set; }

        public override void Mapping(Profile profile)
        {
            profile.CreateMap<PermissionCreateCommand, Permission>()
                .ForMember(x => x.ApiResources, opt => opt.MapFrom(x => x.PermissionResources!.Where(x=>x.Type=="API")!.Select(x => new PermissionApiResource { ApiResourceId = x.Id })))
                .ForMember(x => x.UiResources, opt => opt.MapFrom(x => x.PermissionResources!.Where(x => x.Type == "UI").Select(x => new PermissionUiResource { UiResourceId = x.Id })))
                ;
        }
    }
    public class PermissionCreateValidator : AbstractValidator<PermissionCreateCommand>
    {
        public PermissionCreateValidator(IIdentityDbContext context)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .MaximumLength(150)
                .Matches(@"^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$")
                .WithMessage("Permission key must use resource.action format.")
                .MustAsync(async (code, cancellationToken) =>
                {
                    var normalized = code.Trim().ToLowerInvariant();

                    return !await context.Permissions
                        .AnyAsync(
                            permission => permission.Code == normalized,
                            cancellationToken);
                })
                .WithMessage("Permission key is already in use.");

            RuleFor(x => x.ResourceId)
                .NotEmpty();

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                {
                    var resourceCode = await context.Resources
                        .AsNoTracking()
                        .Where(resource =>
                            resource.Id == command.ResourceId &&
                            resource.IsActive)
                        .Select(resource => resource.Code)
                        .SingleOrDefaultAsync(cancellationToken);

                    if (string.IsNullOrWhiteSpace(resourceCode))
                        return false;

                    var normalized = command.Code
                        .Trim()
                        .ToLowerInvariant();

                    return normalized.StartsWith(
                        $"{resourceCode.Trim().ToLowerInvariant()}.",
                        StringComparison.Ordinal);
                })
                .WithMessage("Permission resource must match its permission key.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);
        }
    }
    public class PermissionCreateHandler : ICommandHandler<PermissionCreateCommand, Guid>
    {
        private readonly IIdentityDbContext _context;
        private readonly IMapper _mapper;

        public PermissionCreateHandler(IIdentityDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Guid> Handle(PermissionCreateCommand command, CancellationToken cancellationToken)
        {

            Permission entity = _mapper.Map<Permission>(command);
            entity.SetKey(command.Code);
            await _context.Permissions.AddAsync(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
