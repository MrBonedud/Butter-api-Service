namespace Core.Interfaces;

using Core.Entities;



public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();

    DateTime GetAccessTokenExpiryUtc();
}
