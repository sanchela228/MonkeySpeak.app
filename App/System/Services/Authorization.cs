using App.Configurations;

namespace App.System.Services;

public class Authorization(Modules.Network net)
{
    public enum AuthState
    {
        Error,
        Success,
        Pending,
        None
    }
    
    public AuthState State { get; set; } = AuthState.None;
    public string ErrorMessage { get; } = "Server not available";
    
    public Modules.Network Network { get; private set; } = net;


    public async void Auth()
    {
        State = AuthState.Pending;
        
        try
        {
            Console.WriteLine($"Старт авторизации");
            
            await AuthRequest();

            throw new Exception("Tralala");
            State = AuthState.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка Авторизации: {ex}");
            State = AuthState.Error;
        }
        finally
        {
            Console.WriteLine($"Авторизация закончена");
        }
    }
    
    protected async Task AuthRequest()
    {
        await Task.Delay(2000); 
    }
}