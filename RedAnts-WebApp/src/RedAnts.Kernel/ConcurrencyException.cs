namespace RedAnts.Domain;

public sealed class ConcurrencyException(string message) : Exception(message);
