using Service.Ann.Batch.Api.Domain.Dtos.Baan;

using Oracle.ManagedDataAccess.Client;
namespace Service.Ann.Batch.Api.Infrastructure.DataAccess.Baan;
public interface IBaanRepository
{
    Task<BaanBatchDto> GetBatchDataAsync(string fo, CancellationToken ct);
}

public sealed class BaanRepository : IBaanRepository
{
    private readonly string _cs;

    public BaanRepository(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("DefaultConnectionOracle");

        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Oracle connection string not found.");
        }

        _cs = cs.Trim().Replace("\r", "").Replace("\n", "");
    }

    public async Task<BaanBatchDto> GetBatchDataAsync(
        string fo,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            using var conn = new OracleConnection(_cs);
            conn.Open();

            using (var cmdSchema = conn.CreateCommand())
            {
                cmdSchema.CommandText = "ALTER SESSION SET CURRENT_SCHEMA = TRITON";
                cmdSchema.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT T$SRNO, T$REF2, T$PDNO
                FROM TTMXCH402100
                WHERE T$FONO = :fo AND ROWNUM = 1";

            cmd.Parameters.Add(":fo", OracleDbType.Varchar2).Value = fo;

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new BaanBatchDto
                {
                    Sr = reader["T$SRNO"]?.ToString()?.Trim() ?? "",
                    Magento = reader["T$REF2"]?.ToString()?.Trim() ?? "",
                    Po = reader["T$PDNO"]?.ToString()?.Trim() ?? ""
                };
            }

            return new BaanBatchDto();
        }, ct);
    }
}