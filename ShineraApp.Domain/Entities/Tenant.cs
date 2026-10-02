using Domain.SharedKernel.Entities;

namespace ShineraApp.Domain.Entities
{
    public sealed class Tenant : FullAuditableEntity
    {
        private Tenant() : base(1)
        {
        }

        public Tenant(
            string name,
            string slug,
            TenantType type)
        {

            Name = name;
            Slug = slug;
            Type = type;

            IsActive = true;
        }

        public string Name { get; private set; }

        public string Slug { get; private set; }

        public TenantType Type { get; private set; }

        public bool IsActive { get; private set; }

        public void UpdateName(string name)
        {
            Name = name.Trim();
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
