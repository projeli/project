using MassTransit;
using Projeli.ProjectService.Domain.Repositories;

namespace Projeli.ProjectService.Infrastructure.Repositories;

public class BusRepository(IBus bus) : IBusRepository
{
    public Task Publish(object message)
    {
        return bus.Publish(message);
    }
}