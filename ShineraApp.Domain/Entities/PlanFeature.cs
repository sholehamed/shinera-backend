using Domain.SharedKernel.Entities;

namespace ShineraApp.Domain.Entities
{
    public sealed class PlanFeature : FullAuditableEntity
    {
        private PlanFeature() : base(4)
        {
        }
        public void Configure(
    bool isEnabled,
    long? limitValue)
        {
            IsEnabled = isEnabled;
            LimitValue = limitValue;
        }
        public PlanFeature(
            Guid planId,
            Guid featureId,
            bool isEnabled,
            long? limitValue = null)
        {
            PlanId = planId;
            FeatureId = featureId;

            IsEnabled = isEnabled;
            LimitValue = limitValue;
        }

        public Guid PlanId { get; private set; }

        public Guid FeatureId { get; private set; }

        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Null means unlimited when the feature is of type Limit.
        /// </summary>
        public long? LimitValue { get; private set; }

        public Plan Plan { get; private set; } = default!;

        public Feature Feature { get; private set; } = default!;
    }
}
