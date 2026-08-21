using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinica.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCobrancasWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cobrancas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MensalidadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Provedor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProvedorCobrancaId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    PixCopiaECola = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BoletoLinhaDigitavel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiraEmUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PagaEmUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobrancas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "eventos_pagamento_webhook",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provedor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EventoId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TipoEvento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Processado = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessadoEmUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ErroProcessamento = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_pagamento_webhook", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_provedor_id",
                table: "cobrancas",
                columns: new[] { "Provedor", "ProvedorCobrancaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cobrancas_tenant_mensalidade",
                table: "cobrancas",
                columns: new[] { "ClinicaId", "MensalidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_nao_processados",
                table: "eventos_pagamento_webhook",
                column: "Processado",
                filter: "\"Processado\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_tenant_provedor_evento",
                table: "eventos_pagamento_webhook",
                columns: new[] { "ClinicaId", "Provedor", "EventoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cobrancas");

            migrationBuilder.DropTable(
                name: "eventos_pagamento_webhook");
        }
    }
}
