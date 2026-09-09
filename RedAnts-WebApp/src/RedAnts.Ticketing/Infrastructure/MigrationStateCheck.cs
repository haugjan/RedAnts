using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace RedAnts.Ticketing.Infrastructure;

public sealed class MigrationStateCheck(IKeyValueService keyValueService, ILogger logger)
{
    public void EnsureStoredStateIsKnown(Upgrader upgrader, string database)
    {
        var state = keyValueService.GetValue(upgrader.StateValueKey);
        if (string.IsNullOrEmpty(state)) return;

        var plan = upgrader.Plan;
        if (plan.Transitions.ContainsKey(state) || string.Equals(state, plan.FinalState, StringComparison.Ordinal)) return;

        var diagnosis = Diagnosis(plan.Name, state, database, plan.FinalState);
        logger.LogError("{MigrationDiagnosis}", diagnosis);
        throw new DomainException(diagnosis);
    }

    public static string Diagnosis(string plan, string state, string database, string finalState) =>
        $"{plan} migration plan does not know the stored state \"{state}\" of database \"{database}\"; " +
        $"last known step is \"{finalState}\". Reset the state or deploy the branch that owns it.";
}
