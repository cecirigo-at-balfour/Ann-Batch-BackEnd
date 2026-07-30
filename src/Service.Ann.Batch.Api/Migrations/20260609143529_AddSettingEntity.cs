using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Ann.Batch.Api.Migrations;

/// <inheritdoc />
public partial class AddSettingEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "setting_entity",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),

                config_key = table.Column<string>(maxLength: 100, nullable: false),

                config_value = table.Column<string>(maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_setting_entity", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_setting_entity_config_key",
            table: "setting_entity",
            column: "config_key",
            unique: true);
    }
}
