namespace FitTrack.Application.Abstractions.Security;

public interface ITokenService
{
    string CreateToken(User user);
}
