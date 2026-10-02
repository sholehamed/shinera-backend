namespace ShineraApp.Domain.Errors
{
    public static class BranchErrors
    {
        public static Error NotFound(Guid branchId) =>
            Error.NotFound(
                "Branch.NotFound",
                $"شعبه با شناسه '{branchId}' پیدا نشد.");
    }
}
