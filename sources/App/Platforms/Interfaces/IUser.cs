namespace Platforms.Interfaces;

public interface IUser : IPlatformService
{
    bool IsAdministrator();
    bool CanWriteTo(string folder);
}