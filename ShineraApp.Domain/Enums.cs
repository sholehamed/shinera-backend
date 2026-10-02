namespace ShineraApp.Domain
{
    public enum TenantType
    {
        System = 1,
        Solo = 2,
        Salon = 3
    }
    public enum PlanAudience
    {
        Solo = 1,
        Salon = 2
    }

    public enum FeatureValueType
    {
        Toggle = 1,
        Limit = 2
    }

    public enum BillingPeriod
    {
        Monthly = 1,
        Yearly = 2
    }
}
