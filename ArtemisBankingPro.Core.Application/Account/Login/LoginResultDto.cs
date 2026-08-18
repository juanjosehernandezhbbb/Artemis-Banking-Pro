namespace ArtemisBankingPro.Core.Application.Account.Login
{
    public record LoginResultDto(string UserId, string UserName, string Nombre, string Apellido, string Role, int? CommerceId = null);
}