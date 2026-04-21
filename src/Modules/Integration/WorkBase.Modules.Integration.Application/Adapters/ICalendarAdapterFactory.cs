using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Application.Adapters;

public interface ICalendarAdapterFactory
{
    ICalendarAdapter GetAdapter(IntegrationProvider provider);
}
