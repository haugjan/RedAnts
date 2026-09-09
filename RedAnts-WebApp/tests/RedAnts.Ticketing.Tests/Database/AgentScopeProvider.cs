using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using IScope = Umbraco.Cms.Infrastructure.Scoping.IScope;
using IScopeProvider = Umbraco.Cms.Infrastructure.Scoping.IScopeProvider;
using IsolationLevel = System.Data.IsolationLevel;

namespace RedAnts.Ticketing.Tests.Database;

internal sealed class AgentScopeProvider(IUmbracoDatabase database) : IScopeProvider
{
    public ISqlContext SqlContext => throw new NotSupportedException("The agent database runs without an Umbraco SQL context.");

    public IScopeContext? Context => null;

    public IScope CreateScope(
        IsolationLevel isolationLevel = IsolationLevel.Unspecified,
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        IEventDispatcher? eventDispatcher = null,
        IScopedNotificationPublisher? scopedNotificationPublisher = null,
        bool? scopeFileSystems = null,
        bool callContext = false,
        bool autoComplete = false) => new AgentScope(database);

    public IScope CreateDetachedScope(
        IsolationLevel isolationLevel = IsolationLevel.Unspecified,
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        IEventDispatcher? eventDispatcher = null,
        IScopedNotificationPublisher? scopedNotificationPublisher = null,
        bool? scopeFileSystems = null) => new AgentScope(database);

    public void AttachScope(IScope other, bool callContext = false) { }

    public IScope DetachScope() => throw new NotSupportedException("The agent scope provider keeps no scope stack.");

    ICoreScope ICoreScopeProvider.CreateCoreScope(
        IsolationLevel isolationLevel,
        RepositoryCacheMode repositoryCacheMode,
        IEventDispatcher? eventDispatcher,
        IScopedNotificationPublisher? scopedNotificationPublisher,
        bool? scopeFileSystems,
        bool callContext,
        bool autoComplete) => new AgentScope(database);

    IQuery<T> ICoreScopeProvider.CreateQuery<T>() => throw new NotSupportedException("The agent scope provider builds no queries.");
}

internal sealed class AgentScope(IUmbracoDatabase database) : IScope
{
    public IUmbracoDatabase Database => database;

    public ISqlContext SqlContext => throw new NotSupportedException("The agent database runs without an Umbraco SQL context.");

    public IsolatedCaches IsolatedCaches => throw new NotSupportedException("The agent scope caches nothing.");

    public IScopedNotificationPublisher Notifications => throw new NotSupportedException("The agent scope publishes no notifications.");

    public RepositoryCacheMode RepositoryCacheMode => RepositoryCacheMode.Unspecified;

    public int Depth => 0;

    public ILockingMechanism Locks => throw new NotSupportedException("The agent scope takes no locks.");

    public void ReadLock(params int[] lockIds) { }

    public void ReadLock(TimeSpan timeout, int lockId) { }

    public void WriteLock(params int[] lockIds) { }

    public void WriteLock(TimeSpan timeout, int lockId) { }

    public void EagerReadLock(params int[] lockIds) { }

    public void EagerReadLock(TimeSpan timeout, int lockId) { }

    public void EagerWriteLock(params int[] lockIds) { }

    public void EagerWriteLock(TimeSpan timeout, int lockId) { }

    public bool Complete() => true;

    public Guid InstanceId { get; } = Guid.NewGuid();

    public int CreatedThreadId => Environment.CurrentManagedThreadId;

    public void Dispose() { }
}
