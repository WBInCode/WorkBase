using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Application.Adapters;

public interface IChatAdapterFactory
{
    IChatAdapter GetAdapter(IntegrationProvider provider);
}
