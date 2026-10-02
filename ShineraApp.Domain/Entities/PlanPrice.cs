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

            if (planId == Guid.Empty)
                throw new ArgumentException("A plan is required.", nameof(planId));
            if (!Enum.IsDefined(billingPeriod))
                throw new ArgumentOutOfRangeException(nameof(billingPeriod));
            ArgumentException.ThrowIfNullOrWhiteSpace(currency);
            var normalizedCurrency = currency.Trim().ToUpperInvariant();
            if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(c => c < 'A' || c > 'Z'))
                throw new ArgumentException("Use a three-letter currency code.", nameof(currency));

            PlanId = planId;
            BillingPeriod = billingPeriod;
            ChangeAmount(amount);
            Currency = normalizedCurrency;

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
