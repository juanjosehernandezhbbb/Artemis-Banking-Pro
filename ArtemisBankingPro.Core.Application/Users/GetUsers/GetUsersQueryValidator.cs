using FluentValidation;
using System.Linq;

namespace ArtemisBankingPro.Core.Application.Users.GetUsers
{
    public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20);
            RuleFor(x => x.Role)
                .Must(r => r == null || new[] { "administrador", "cajero", "cliente" }.Contains(r.ToLower()))
                .WithMessage("El parámetro role solo puede ser administrador, cajero o cliente.");
        }
    }
}