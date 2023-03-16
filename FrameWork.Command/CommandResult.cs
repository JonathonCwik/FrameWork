namespace FrameWork.Command;

public class CommandResult<T>
{
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public T? SuccessData { get; set; }
    public string? Reason { get; set; }

    public static CommandResult<T> Unsuccessful(string message, string reason) {
        return new CommandResult<T> { 
            IsSuccessful = false,
            Message = message,
            Reason = reason
        };
    }

    public static CommandResult<T> Successful(T successData) {
        return new CommandResult<T>() {
            IsSuccessful = true,
            SuccessData = successData
        };
    }
}