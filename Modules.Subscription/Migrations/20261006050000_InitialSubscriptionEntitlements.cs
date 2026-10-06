using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.System.Subscription.Migrations;

public partial class InitialSubscriptionEntitlements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Features",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Kind = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Features", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Plans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Plans", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PlanFeatures",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                LimitValue = table.Column<int>(type: "int", nullable: true),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlanFeatures", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlanFeatures_Features_FeatureId",
                    column: x => x.FeatureId,
                    principalTable: "Features",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PlanFeatures_Plans_PlanId",
                    column: x => x.PlanId,
                    principalTable: "Plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Subscriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ExternalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Subscriptions_Plans_PlanId",
                    column: x => x.PlanId,
                    principalTable: "Plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Features_CreatedAt",
            table: "Features",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Features_Key",
            table: "Features",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Plans_CreatedAt",
            table: "Plans",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Plans_Key",
            table: "Plans",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PlanFeatures_CreatedAt",
            table: "PlanFeatures",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_PlanFeatures_FeatureId",
            table: "PlanFeatures",
            column: "FeatureId");

        migrationBuilder.CreateIndex(
            name: "IX_PlanFeatures_PlanId_FeatureId",
            table: "PlanFeatures",
            columns: new[] { "PlanId", "FeatureId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_CreatedAt",
            table: "Subscriptions",
            column: "CreatedAt",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_PlanId",
            table: "Subscriptions",
            column: "PlanId");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_TenantId",
            table: "Subscriptions",
            column: "TenantId",
            unique: true,
            filter: "[Status] = 2");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_TenantId_StartedAtUtc",
            table: "Subscriptions",
            columns: new[] { "TenantId", "StartedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PlanFeatures");
        migrationBuilder.DropTable(name: "Subscriptions");
        migrationBuilder.DropTable(name: "Features");
        migrationBuilder.DropTable(name: "Plans");
    }
}
