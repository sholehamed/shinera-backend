using FluentValidation;
namespace ShineraApp.Application.Features.Registration;

public sealed class RegisterWorkspaceValidator : AbstractValidator<RegisterWorkspaceCommand>
{
    public RegisterWorkspaceValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.PlanKey).NotEmpty().MaximumLength(64);
        RuleFor(x => x.BillingCycle).Must(x => x is "monthly" or "yearly");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Mobile).NotEmpty().Matches("^09[0-9]{9}$");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Slug).NotEmpty().MinimumLength(3).MaximumLength(40)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .Must(x => x is not ("www" or "api" or "admin" or "app" or "system" or "support"));
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Phone).NotNull().MaximumLength(20);
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(400);
        RuleFor(x => x.PostalCode).NotNull().MaximumLength(20);
        RuleFor(x => x.Instagram).NotNull().MaximumLength(80);
        RuleFor(x => x.AcceptTerms).Equal(true);
        RuleFor(x => x.AcceptPrivacy).Equal(true);
    }
}
