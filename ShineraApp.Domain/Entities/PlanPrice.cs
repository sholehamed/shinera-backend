using Domain.SharedKernel.Entities;

namespace ShineraApp.Domain.Entities
{
    public sealed class PlanPrice : FullAuditableEntity
    {
        private PlanPrice():base(6)
        {
        }
        public void ChangeAmount(decimal amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }
        public PlanPrice(
            Guid planId,
            BillingPeriod billingPeriod,
            decimal amount,
            string currency)
        {

            PlanId = planId;
            BillingPeriod = billingPeriod;
            Amount = amount;
            Currency = currency.ToUpperInvariant();

            IsActive = true;
        }

        public Guid PlanId { get; private set; }

        public BillingPeriod BillingPeriod { get; private set; }

        public decimal Amount { get; private set; }

        public string Currency { get; private set; } = default!;

        public bool IsActive { get; private set; }

        public Plan Plan { get; private set; } = default!;
    }
}
