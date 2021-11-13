using FrameWork.Interfaces;

using Console = System.Console;

namespace FrameWork.Logger.ConsoleLogger;

public class ConsoleLogger : ILogger
{
    public void Debug(string message) {
        Console.WriteLine($"DEBUG - {DateTime.Now} - {message}");
    }

    public void Info(string message) {
        Console.WriteLine($"INFO - {DateTime.Now} - {message}");
    }

    public void Warn(string message, Exception? exception = null) {
        Console.WriteLine($"WARN! - {DateTime.Now} - {message}");
        if (exception != null) {
            Console.WriteLine($"    {exception.Message}");
            Console.WriteLine($"{exception.StackTrace}");
        }
    }

    public void Error(string message, Exception exception) {
        Console.WriteLine($"ERROR!!! - {DateTime.Now} - {message}");
        if (exception != null) {
            Console.WriteLine($"    {exception.Message}");
            Console.WriteLine($"{exception.StackTrace}");
        }
    }
}
