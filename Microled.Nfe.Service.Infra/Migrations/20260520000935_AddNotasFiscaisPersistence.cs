using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microled.Nfe.Service.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddNotasFiscaisPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocolo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    numero_nota = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    codigo_verificacao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    numero_lote = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    numero_rps = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    serie_rps = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    inscricao_prestador = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cnpj_prestador = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    cpf_cnpj_tomador = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    xml = table.Column<string>(type: "text", nullable: true),
                    pdf = table.Column<byte[]>(type: "bytea", nullable: true),
                    data_emissao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    data_cancelamento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_por = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    alterado_por = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_fiscais", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_cnpj_prestador",
                table: "notas_fiscais",
                column: "cnpj_prestador");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_cpf_cnpj_tomador",
                table: "notas_fiscais",
                column: "cpf_cnpj_tomador");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_criado_em",
                table: "notas_fiscais",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_data_emissao",
                table: "notas_fiscais",
                column: "data_emissao");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_inscricao_prestador_serie_rps_numero_rps",
                table: "notas_fiscais",
                columns: new[] { "inscricao_prestador", "serie_rps", "numero_rps" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_numero_nota",
                table: "notas_fiscais",
                column: "numero_nota");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_protocolo",
                table: "notas_fiscais",
                column: "protocolo");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_status",
                table: "notas_fiscais",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notas_fiscais");
        }
    }
}
