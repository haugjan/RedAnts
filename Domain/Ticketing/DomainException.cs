namespace RedAnts.Domain.Ticketing;

/// <summary>Thrown when a ticketing domain invariant is violated.</summary>
public class DomainException(string message) : Exception(message);
