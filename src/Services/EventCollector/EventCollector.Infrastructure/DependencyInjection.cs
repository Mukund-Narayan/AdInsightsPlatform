using EventCollector.Domain.Interfaces;
using EventCollector.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace EventCollector.Infrastructure;

/// <summary>
/// Infrastructure DI registration for the EventCollector service.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEventCollectorInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<KafkaProducerFactory>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        return services;
    }
}
