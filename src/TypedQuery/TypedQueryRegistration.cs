namespace TypedQuery;

/// <summary>
/// Internal registration record for a query in the builder.
/// </summary>
internal sealed record TypedQueryRegistration(
    Type QueryType,
    Type ResultType,
    object QueryInstance
);
