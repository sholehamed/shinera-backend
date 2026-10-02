namespace ShineraApp.Domain.Errors
{
    public static class PlanErrors
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                "Plan.NotFound",
                $"پلن با شناسه '{id}' پیدا نشد.");

        public static Error DuplicateCode(string code) =>
            Error.Conflict(
                "Plan.DuplicateCode",
                $"پلن با کد '{code}' قبلاً ثبت شده است.");

        public static Error InvalidName =>
            Error.Validation(
                "Plan.InvalidName",
                "نام پلن معتبر نیست.");
    }
}
