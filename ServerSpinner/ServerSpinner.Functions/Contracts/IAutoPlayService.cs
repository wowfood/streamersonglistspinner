namespace ServerSpinner.Functions.Contracts;

public interface IAutoPlayService
{
    Task HandleAsync(Guid streamerId, string messageType, string payloadJson);
}
