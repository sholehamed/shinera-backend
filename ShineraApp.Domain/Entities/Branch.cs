using Domain.SharedKernel.Common;
using Domain.SharedKernel.Entities;

namespace ShineraApp.Domain.Entities
{
    public sealed class Branch : FullAuditableEntity, IMustHaveTenant
    {
        private Branch() : base(2)
        {
        }

        public Branch(
            Guid tenantId,
            string name,
            bool isMain)
        {

            TenantId = tenantId;
            Name = name;
            IsMain = isMain;

            IsActive = true;
        }

        public Guid TenantId { get; private set; }

        public string Name { get; private set; }

        public bool IsMain { get; private set; }

        public bool IsActive { get; private set; }

        public void SetMain(bool isMain)
        {
            IsMain = isMain;
        }

        public void Update(string name)
        {
            Name = name.Trim();
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }
    }
}
