using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Entities;

namespace ServerSpinner.Functions.Contracts;

public interface ISettingsMapper
{
    SettingsDto ToDto(StreamerSettings settings);
    void Apply(SettingsDto dto, StreamerSettings settings);
}
