using AuthService.Api.Dtos;
using FluentValidation;
using Shared.Contracts;

namespace AuthService.Api.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role).Must(role => Roles.All.Contains(role))
            .WithMessage($"Role phải là một trong: {string.Join(", ", Roles.All)}.");
    }
}
