namespace ServerSpinner.Functions.Contracts;

public interface ISpinnerStateService
{
    Task UpdateAsync(Guid streamerId, string messageType, string payloadJson);
}
