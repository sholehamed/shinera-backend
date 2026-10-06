using Domain.SharedKernel.Common;
using Domain.SharedKernel.Entities;

namespace Modules.System.Identity.Domain.Entities;

public enum PermissionSubjectType
{
    Role = 1,
    User = 2
}

public enum PermissionScopeType
{
    Tenant = 1,
    Branch = 2,
    Own = 3,
    Child = 4
}

public sealed class PermissionAssignment : AuditableEntity, IMustHaveTenant
{
    public PermissionAssignment() : base(22)
    {
    }

    public PermissionAssignment(
        Guid tenantId,
        Guid permissionId,
        PermissionSubjectType subjectType,
        Guid subjectId,
        PermissionScopeType scopeType,
        Guid? scopeReferenceId = null)
        : base(22)
    {
        TenantId = tenantId;
        PermissionId = permissionId;
        SubjectType = subjectType;
        SubjectId = subjectId;
        ScopeType = scopeType;
        ScopeReferenceId = scopeReferenceId;
    }

    public Guid TenantId { get; set; }
    public Guid PermissionId { get; set; }
    public PermissionSubjectType SubjectType { get; set; }
    public Guid SubjectId { get; set; }
    public PermissionScopeType ScopeType { get; set; }
    public Guid? ScopeReferenceId { get; set; }
    public bool IsActive { get; set; } = true;

    public Permission Permission { get; set; } = default!;
}
