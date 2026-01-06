using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSpartan.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashbackPercentual",
                table: "Produtos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashbackTotal",
                table: "Pedidos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CashbackValor",
                table: "ItensPedido",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashbackPercentual",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "CashbackTotal",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CashbackValor",
                table: "ItensPedido");
        }
    }
}
