using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Entities;

namespace ServerSpinner.Functions.Contracts;

public interface ISpinnerConfigMapper
{
    SpinnerConfigResponse ToConfigResponse(StreamerSettings settings);
}
