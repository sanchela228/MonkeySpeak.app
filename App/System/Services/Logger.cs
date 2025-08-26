using App;
using App.System;

namespace App.System.Services;

public static class Logger
{
    private const string NameErrorsFile = "Errors.log";
    private const string NameLastLaunchLogsFile = "LastLaunchLogs.log";
    private const string NameLogsFile = "Logs.log";

    private static Guid LastSessionKeyTouch;


    public static void Write(Type type, string message)
    {
#if !DEBUG
        var lastLogFile = Context.Instance.LogsDataDirectory + "/" + NameLastLaunchLogsFile;
        var errorsLogFile = Context.Instance.LogsDataDirectory + "/" + NameErrorsFile;
        var logsLogFile = Context.Instance.LogsDataDirectory + "/" + NameLogsFile;
        
        
        if (LastSessionKeyTouch != Context.Instance.CurrentSessionToken)
        {
            LastSessionKeyTouch = Context.Instance.CurrentSessionToken;
            CreateOrClearLogFile(lastLogFile);
        }
        
        try
        {
            using (StreamWriter writer = new StreamWriter(lastLogFile, true))
            {
                writer.WriteLine($"{DateTime.Now:dd.mm.yyyy HH:mm:ss} | {type.ToString()} | - {message}");
            }
            
            using (StreamWriter writer = new StreamWriter(logsLogFile, true))
            {
                writer.WriteLine($"{DateTime.Now:dd.mm.yyyy HH:mm:ss} | {type.ToString()} | - {message}");
            }

            if (type == Type.Error)
            {
                using (StreamWriter writer = new StreamWriter(errorsLogFile, true))
                {
                    writer.WriteLine($"{DateTime.Now:dd.mm.yyyy HH:mm:ss} | ERROR | - {message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
        }
#endif
#if DEBUG
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} | {type.ToString()} | - {message}");
#endif   
    }
    
    public static void Write(Type type, string message, Exception exc)
    {
#if !DEBUG
        var lastLogFile = Context.Instance.LogsDataDirectory + "/" + NameLastLaunchLogsFile;
        var errorsLogFile = Context.Instance.LogsDataDirectory + "/" + NameErrorsFile;
        var logsLogFile = Context.Instance.LogsDataDirectory + "/" + NameLogsFile;
        
        
        if (LastSessionKeyTouch != Context.Instance.CurrentSessionToken)
        {
            LastSessionKeyTouch = Context.Instance.CurrentSessionToken;
            CreateOrClearLogFile(lastLogFile);
        }
        
        try
        {
            using (StreamWriter writer = new StreamWriter(lastLogFile, true))
            {
                writer.WriteLine($"{DateTime.Now:dd.mm.yyyy HH:mm:ss} | {type.ToString()} | - {message} - {exc.Message}");
            }
            
            using (StreamWriter writer = new StreamWriter(logsLogFile, true))
            {
                writer.WriteLine($"{DateTime.Now:dd.mm.yyyy HH:mm:ss} | {type.ToString()} | - {message} - {exc.Message}");
            }

            if (type == Type.Error)
            {
                using (StreamWriter writer = new StreamWriter(errorsLogFile, true))
                {
                    writer.WriteLine($"{DateTime.Now:dd.mm.yyyy HH:mm:ss} | ERROR | - {message} - {exc.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка записи в лог: {ex.Message}");
        }
#endif    
#if DEBUG
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} | {type.ToString()} | - {message}");
#endif   
    }

    static void CreateOrClearLogFile(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.SetLength(0);
            }
        }
        catch (Exception ex)
        {
        }
    }

    public enum Type
    {
        Error,
        Warning,
        Info
    } 
}