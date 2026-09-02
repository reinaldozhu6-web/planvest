using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace PlanVest.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609010001_InitialIdentity")]
public partial class InitialIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                LastLoginAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Users", value => value.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users",
            column: "NormalizedEmail",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("Users");
}
