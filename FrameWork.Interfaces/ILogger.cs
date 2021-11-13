namespace FrameWork.Interfaces
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warn(string message, Exception? exception = null);
        void Error(string message, Exception exception);
    }
}