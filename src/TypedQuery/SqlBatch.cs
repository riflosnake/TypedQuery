using System.Data.Common;

namespace TypedQuery;

/// <summary>
/// Represents a compiled SQL batch with all queries combined.
/// </summary>
internal sealed record SqlBatch(
    string Sql,
    IReadOnlyList<DbParameter> Parameters,
    IReadOnlyList<Type> ResultTypes,
    IReadOnlyList<Type> QueryTypes
);
