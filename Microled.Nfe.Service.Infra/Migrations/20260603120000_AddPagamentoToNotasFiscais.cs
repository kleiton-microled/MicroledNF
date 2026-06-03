using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microled.Nfe.Service.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddPagamentoToNotasFiscais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "pago",
                table: "notas_fiscais",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "data_pagamento",
                table: "notas_fiscais",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_depositado",
                table: "notas_fiscais",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pago",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "data_pagamento",
                table: "notas_fiscais");

            migrationBuilder.DropColumn(
                name: "valor_depositado",
                table: "notas_fiscais");
        }
    }
}
