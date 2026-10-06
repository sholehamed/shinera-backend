using Application.SharedKernel.Abstractions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace Infrastructure.SharedKernel.Concurrency;

public sealed class SqlServerStaffBookingConcurrencyGuard
    : IStaffBookingConcurrencyGuard
{
    public async Task<bool> TryAcquireAsync(
        DatabaseFacade database,
        Guid tenantId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var currentTransaction = database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "A database transaction must be active before acquiring the staff booking lock.");

        var connection = database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = currentTransaction.GetDbTransaction();
        command.CommandText =
            """
            SELECT [Id]
            FROM [Staff] WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
            WHERE [TenantId] = @tenantId
              AND [Id] = @staffId;
            """;

        AddGuidParameter(command, "@tenantId", tenantId);
        AddGuidParameter(command, "@staffId", staffId);

        var result = await command.ExecuteScalarAsync(
            cancellationToken);

        return result is not null &&
               result is not DBNull;
    }

    private static void AddGuidParameter(
        DbCommand command,
        string name,
        Guid value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Guid;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
