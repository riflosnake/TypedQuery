using Dapper;

namespace TypedQuery;

/// <summary>
/// Represents a compiled SQL batch with all queries combined.
/// </summary>
internal sealed record SqlBatch(
    string Sql,
    DynamicParameters Parameters,
    IReadOnlyList<Type> ResultTypes,
    IReadOnlyList<Type> QueryTypes
);
