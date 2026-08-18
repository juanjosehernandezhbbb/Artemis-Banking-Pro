using FluentValidation;

namespace ArtemisBankingPro.Core.Application.Users.ChangeUserStatus
{
    public class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
    {
        public ChangeUserStatusCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Status)
                .NotNull()
                .WithMessage("El campo 'status' es obligatorio.");
        }
    }
}