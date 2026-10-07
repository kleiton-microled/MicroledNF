using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Microled.Nfe.Service.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddTomadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tomadores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cpf_cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    razao_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    inscricao_municipal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    inscricao_estadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tipo_logradouro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    codigo_municipio = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tomadores", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tomadores_cpf_cnpj",
                table: "tomadores",
                column: "cpf_cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tomadores_razao_social",
                table: "tomadores",
                column: "razao_social");

            // Carga inicial: tomadores que antes ficavam fixos no front-end (local-clients.storage.ts).
            migrationBuilder.Sql(@"
INSERT INTO tomadores (id, cpf_cnpj, razao_social, inscricao_municipal, email, tipo_logradouro, logradouro, numero, complemento, bairro, uf, codigo_municipio, cep, criado_em) VALUES
('6f0c1a52-3d1e-4a6b-9a01-000000000001', '58138058003100', 'EUDMARCO S/A SERVICOS E COMERCIO INTERNACIONAL', NULL, 'xml@abainfra.com.br', 'AV', 'Senador Dantas', '206', NULL, 'Macuco', 'SP', '3548500', '11015300', now()),
('6f0c1a52-3d1e-4a6b-9a01-000000000002', '02390435000115', 'ECOPORTO SANTOS S/A', NULL, 'administrativoti.op@ecoportosantos.com.br', 'AV', 'ENGENHEIRO ALVES FREIRE', 'S/Nº', NULL, 'CAIS SABOO', 'SP', '3548500', '11010230', now()),
('6f0c1a52-3d1e-4a6b-9a01-000000000003', '02126914000129', 'MICROLED INFORMATICA E SERVICOS LTDA', '3.768.428-0', 'ipsilva@microled.com.br', 'AV', 'IRAI', '00075', 'CJ 21 TORRE A', 'INDIANOPOLIS', 'SP', '3550308', '04082000', now()),
('6f0c1a52-3d1e-4a6b-9a01-000000000004', '09218626000143', 'SPLOGICA SISTEMAS E CONSULTORIA LTDA', '3.698.180-0', 'ipsilva@microled.com.br', 'AV', 'IRAI', '00075', 'CJ 21 TORRE A', 'INDIANOPOLIS', 'SP', '3550308', '04082000', now()),
('6f0c1a52-3d1e-4a6b-9a01-000000000005', '53730495000170', 'TERMARES TERMINAIS MARITIMOS ESPECIALIZADOS LTDA', '3.698.180-0', 'administrativoti.op@ecoportosantos.com.br', 'AV', 'C DO SABOO, S/N', 'S/N', 'CAIS PATIO 1 2 E 3', 'SABOO', 'SP', '3550308', '11010970', now()),
('6f0c1a52-3d1e-4a6b-9a01-000000000006', '45455869000169', 'M6 CARGO LOGISTICA LTDA', NULL, 'FISCAL@TECNOCONTABILIDADE.COM.BR', 'PC', 'PC REPUBLICA', 'S/N', 'SALA 42', 'CENTRO', 'SP', '3550308', '11013922', now())
ON CONFLICT (cpf_cnpj) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tomadores");
        }
    }
}
