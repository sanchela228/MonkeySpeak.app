namespace Platforms.Interfaces;

public interface ISecureStorage : IPlatformService
{
    void Save(string key, string value);
    string Load(string key);
    void Delete(string key);
}