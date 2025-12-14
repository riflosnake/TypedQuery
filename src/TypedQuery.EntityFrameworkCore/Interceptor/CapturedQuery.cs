using System.Data.Common;

namespace TypedQuery.EntityFrameworkCore.Interceptor;

/// <summary>
/// Represents a captured EF Core query with SQL and parameters.
/// </summary>
internal sealed record CapturedQuery(string Sql, DbParameter[] Parameters);
