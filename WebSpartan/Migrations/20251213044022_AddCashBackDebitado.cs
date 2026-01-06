using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSpartan.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBackDebitado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashbackTotal",
                table: "Pedidos");

            migrationBuilder.AddColumn<bool>(
                name: "CashbackDebitado",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashbackDebitado",
                table: "Pedidos");

            migrationBuilder.AddColumn<decimal>(
                name: "CashbackTotal",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
