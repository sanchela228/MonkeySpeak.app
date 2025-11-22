using System.Net.NetworkInformation;

namespace App.System.Services;

public class ComputerIdentity
{
    public static string GetMacAddress()
    {
        try
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up && 
                                       nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            if (networkInterface != null)
            {
                var macAddress = networkInterface.GetPhysicalAddress();
                return macAddress.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения MAC-адреса: {ex.Message}");
        }
        
        return string.Empty;
    }
}