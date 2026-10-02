using Domain.SharedKernel.Entities;

namespace ShineraApp.Domain.Entities
{
    public sealed class Feature : FullAuditableEntity
    {
        private readonly List<PlanFeature> _plans = [];

        private Feature() : base(5)
        {
        }
        public void Configure(
    string? unit,
    int displayOrder)
        {
            Unit = unit;
            DisplayOrder = displayOrder;
        }

        public void Update(
            string name,
            string? description,
            string? unit,
            int displayOrder,
            bool isVisible)
        {
            Name = name.Trim();
            Description = description;
            Unit = unit;
            DisplayOrder = displayOrder;
            IsVisible = isVisible;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }
        public Feature(
            string code,
            string name,
            FeatureValueType valueType,
            string? description = null)
        {

            Code = code;
            Name = name;
            Description = description;
            ValueType = valueType;

            IsActive = true;
            IsVisible = true;
        }

        public string Code { get; private set; } = default!;

        public string Name { get; private set; } = default!;

        public string? Description { get; private set; }

        public FeatureValueType ValueType { get; private set; }

        /// <summary>
        /// Example:
        /// شعبه
        /// کارمند
        /// پیامک
        /// نوبت
        /// </summary>
        public string? Unit { get; private set; }

        public int DisplayOrder { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsVisible { get; private set; }

        public IReadOnlyCollection<PlanFeature> Plans => _plans;
    }
}
