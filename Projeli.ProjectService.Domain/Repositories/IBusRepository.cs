namespace Projeli.ProjectService.Domain.Repositories;

public interface IBusRepository
{
    Task Publish(object @event);
}