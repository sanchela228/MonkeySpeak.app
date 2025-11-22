namespace Platforms;

public interface IPlatformService
{
    public Platforms ServicePlatform();
    public bool IsCritical();
}