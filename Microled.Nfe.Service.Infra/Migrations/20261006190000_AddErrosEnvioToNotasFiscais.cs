using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microled.Nfe.Service.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddErrosEnvioToNotasFiscais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "erros_envio",
                table: "notas_fiscais",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "erros_envio",
                table: "notas_fiscais");
        }
    }
}
