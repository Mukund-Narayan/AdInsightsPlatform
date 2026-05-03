using Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AdInsights.Infrastructure.Persistence.Cassandra;

/// <summary>
/// Creates and manages the Apache Cassandra cluster connection and session.
/// Registered as a <c>Singleton</c> — ISession is thread-safe and connection-pooled.
///
/// Connection settings are read from IConfiguration (appsettings / environment variables).
/// </summary>
public sealed class CassandraConnectionFactory : IDisposable
{
    private readonly ICluster _cluster;
    private readonly ISession _session;
    private bool _disposed;

    public CassandraConnectionFactory(
        IConfiguration configuration,
        ILogger<CassandraConnectionFactory> logger)
    {
        var contactPoints = configuration
            .GetSection("Cassandra:ContactPoints")
            .Get<string[]>() ?? ["localhost"];

        var port = configuration.GetValue("Cassandra:Port", 9042);
        var keyspace = configuration.GetValue("Cassandra:Keyspace", "ad_insights");
        var username = configuration["Cassandra:Username"] ?? string.Empty;
        var password = configuration["Cassandra:Password"] ?? string.Empty;

        logger.LogInformation(
            "Connecting to Cassandra at {ContactPoints} keyspace={Keyspace}",
            string.Join(",", contactPoints),
            keyspace);

        var builder = Cluster.Builder()
            .AddContactPoints(contactPoints)
            .WithPort(port)
            .WithLoadBalancingPolicy(new TokenAwarePolicy(new DCAwareRoundRobinPolicy()))
            .WithReconnectionPolicy(new ExponentialReconnectionPolicy(1000, 60000))
            .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalQuorum));

        if (!string.IsNullOrWhiteSpace(username))
        {
            builder.WithCredentials(username, password);
        }

        _cluster = builder.Build();
        _session = _cluster.Connect(keyspace);
    }

    /// <summary>Returns the active Cassandra session for query execution.</summary>
    public ISession CreateSession() => _session;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _cluster.Dispose();
        _disposed = true;
    }
}
