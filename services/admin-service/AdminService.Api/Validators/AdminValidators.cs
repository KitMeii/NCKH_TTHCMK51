using AdminService.Api.Dtos;
using FluentValidation;
using Shared.Contracts;

namespace AdminService.Api.Validators;

public sealed class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role).Must(role => Roles.All.Contains(role))
            .WithMessage($"Role phải là một trong: {string.Join(", ", Roles.All)}.");
    }
}

public sealed class SetConfigRequestValidator : AbstractValidator<SetConfigRequest>
{
    public SetConfigRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(4000);
    }
}
