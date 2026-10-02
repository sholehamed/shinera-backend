using Domain.SharedKernel.Entities;

namespace ShineraApp.Domain.Entities
{
    public sealed class Plan : FullAuditableEntity
    {
        private readonly List<PlanFeature> _features = [];
        private readonly List<PlanPrice> _prices = [];

        private Plan():base(3)
        {
        }

        public Plan(
            string code,
            string name,
            PlanAudience audience,
            string? description = null)
        {

            Code = code;
            Name = name;
            Description = description;
            Audience = audience;

            IsActive = true;
            IsPublic = true;
        }

        public string Code { get; private set; } = default!;

        public string Name { get; private set; } = default!;

        public string? Description { get; private set; }

        public PlanAudience Audience { get; private set; }

        public int DisplayOrder { get; private set; }

        public int TrialDays { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsPublic { get; private set; }

        public IReadOnlyCollection<PlanFeature> Features => _features;

        public IReadOnlyCollection<PlanPrice> Prices => _prices;

        public void Configure(
     int trialDays,
     int displayOrder)
        {
            TrialDays = trialDays;
            DisplayOrder = displayOrder;
        }

        public void Update(
            string name,
            string? description,
            PlanAudience audience,
            int trialDays,
            int displayOrder,
            bool isPublic)
        {
            Name = name.Trim();
            Description = description;
            Audience = audience;
            TrialDays = trialDays;
            DisplayOrder = displayOrder;
            IsPublic = isPublic;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }
    }
}
