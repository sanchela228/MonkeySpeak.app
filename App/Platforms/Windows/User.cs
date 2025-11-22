using System.Security.Principal;
using Platforms.Interfaces;

namespace Platforms.Windows;

public class User : IUser
{
    public Platforms ServicePlatform() => Platforms.Windows;
    public bool IsCritical() => true;
    
    public bool IsAdministrator()
    {
        var wi = WindowsIdentity.GetCurrent();
        var wp = new WindowsPrincipal(wi);
        return wp.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public bool CanWriteTo(string folder)
    {
        try
        {
            var testFile = Path.Combine(folder, $".write_test_{Guid.NewGuid():N}.tmp");
            using (var fs = new FileStream(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                fs.WriteByte(0x42);
            }
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch { return false; }
    }
}
