using System.Data.Common;

namespace TypedQuery.EntityFrameworkCore.Interceptor;

internal sealed class EmptyDataReader : DbDataReader
{
    public static readonly EmptyDataReader Instance = new();
    
    private EmptyDataReader() { }
    
    public override int FieldCount => 0;
    public override bool HasRows => false;
    public override bool IsClosed => true;
    public override int RecordsAffected => 0;
    public override int Depth => 0;
    public override object this[int ordinal] => throw new InvalidOperationException("No data available");
    public override object this[string name] => throw new InvalidOperationException("No data available");
    public override bool GetBoolean(int ordinal) => throw new InvalidOperationException("No data available");
    public override byte GetByte(int ordinal) => throw new InvalidOperationException("No data available");
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new InvalidOperationException("No data available");
    public override char GetChar(int ordinal) => throw new InvalidOperationException("No data available");
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new InvalidOperationException("No data available");
    public override string GetDataTypeName(int ordinal) => throw new InvalidOperationException("No data available");
    public override DateTime GetDateTime(int ordinal) => throw new InvalidOperationException("No data available");
    public override decimal GetDecimal(int ordinal) => throw new InvalidOperationException("No data available");
    public override double GetDouble(int ordinal) => throw new InvalidOperationException("No data available");
    public override System.Collections.IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
    public override Type GetFieldType(int ordinal) => throw new InvalidOperationException("No data available");
    public override float GetFloat(int ordinal) => throw new InvalidOperationException("No data available");
    public override Guid GetGuid(int ordinal) => throw new InvalidOperationException("No data available");
    public override short GetInt16(int ordinal) => throw new InvalidOperationException("No data available");
    public override int GetInt32(int ordinal) => throw new InvalidOperationException("No data available");
    public override long GetInt64(int ordinal) => throw new InvalidOperationException("No data available");
    public override string GetName(int ordinal) => throw new InvalidOperationException("No data available");
    public override int GetOrdinal(string name) => throw new InvalidOperationException("No data available");
    public override string GetString(int ordinal) => throw new InvalidOperationException("No data available");
    public override object GetValue(int ordinal) => throw new InvalidOperationException("No data available");
    public override int GetValues(object[] values) => 0;
    public override bool IsDBNull(int ordinal) => throw new InvalidOperationException("No data available");
    public override bool NextResult() => false;
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    public override bool Read() => false;
}
