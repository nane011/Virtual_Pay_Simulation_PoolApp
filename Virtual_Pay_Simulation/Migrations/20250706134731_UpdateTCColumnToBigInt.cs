using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Virtual_Pay_Simulation.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTCColumnToBigInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "TC",
                table: "Uyeler",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TC",
                table: "Uyeler",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
