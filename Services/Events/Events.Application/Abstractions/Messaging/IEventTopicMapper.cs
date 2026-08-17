namespace Events.Application.Abstractions.Messaging;

public interface IEventTopicMapper
{
    string GetTopicFor<T>();
}
