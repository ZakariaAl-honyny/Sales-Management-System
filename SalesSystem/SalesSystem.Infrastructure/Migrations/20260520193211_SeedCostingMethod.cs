using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCostingMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT SystemSettings ON;
                INSERT INTO SystemSettings (Id, SettingKey, SettingValue, DataType, Category, DisplayName, Description, CreatedAt, UpdatedAt, IsActive, CreatedByUserId, UpdatedByUserId)
                VALUES (1, 'CostingMethod', 'WeightedAverage', 'string', 'Inventory', 'طريقة احتساب التكلفة', 'تحدد كيف يحتسب النظام تكلفة البضاعة في المخزن', GETUTCDATE(), GETUTCDATE(), 1, NULL, NULL);
                SET IDENTITY_INSERT SystemSettings OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM SystemSettings WHERE SettingKey = 'CostingMethod'");
        }
    }
}