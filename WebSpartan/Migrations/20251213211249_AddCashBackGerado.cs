using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSpartan.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBackGerado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CashbackCreditado",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CashbackGerado",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashbackCreditado",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CashbackGerado",
                table: "Pedidos");
        }
    }
}
