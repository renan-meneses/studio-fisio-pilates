using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinica.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clinicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    CNPJ = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    Endereco = table.Column<string>(type: "text", nullable: true),
                    Plano = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contas_pagar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fornecedor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TipoCusto = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_pagar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "planos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "profissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CPF = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    RegistroProfissional = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Especialidades = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Cargo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SalarioBase = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataAdmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataDemissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profissionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Papel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimoLogin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Tema = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Sobrenome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CPF = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    DataNascimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Sexo = table.Column<int>(type: "integer", nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Indicacao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataPrimeiraSessao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PlanoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pacientes_planos_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "planos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "folhas_salariais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Competencia = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ValorBruto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Descontos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiasTrabalhados = table.Column<int>(type: "integer", nullable: false),
                    Faltas = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folhas_salariais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_folhas_salariais_profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pontos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Entrada = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Saida = table.Column<TimeSpan>(type: "interval", nullable: true),
                    AlmocoInicio = table.Column<TimeSpan>(type: "interval", nullable: true),
                    AlmocoFim = table.Column<TimeSpan>(type: "interval", nullable: true),
                    HorasExtras = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pontos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pontos_profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "turmas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TipoSessao = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turmas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_turmas_profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "planos_servicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos_servicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_planos_servicos_planos_PlanoId",
                        column: x => x.PlanoId,
                        principalTable: "planos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_planos_servicos_servicos_ServicoId",
                        column: x => x.ServicoId,
                        principalTable: "servicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mensalidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Competencia = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensalidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mensalidades_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prontuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataAbertura = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prontuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prontuarios_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agendamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHoraInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataHoraFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TipoSessao = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TipoAula = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ValorSessao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agendamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agendamentos_pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agendamentos_profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_agendamentos_turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "turmas_horarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraFim = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turmas_horarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_turmas_horarios_turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evolucoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProntuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    QueixaPrincipal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Avaliacao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Conduta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evolucoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evolucoes_profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evolucoes_prontuarios_ProntuarioId",
                        column: x => x.ProntuarioId,
                        principalTable: "prontuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Presencas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Entrada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Saida = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presencas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presencas_agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_PacienteId",
                table: "agendamentos",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_ProfissionalId",
                table: "agendamentos",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_tenant_inicio",
                table: "agendamentos",
                columns: new[] { "ClinicaId", "DataHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_tenant_profissional",
                table: "agendamentos",
                columns: new[] { "ClinicaId", "ProfissionalId", "DataHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_agendamentos_TurmaId",
                table: "agendamentos",
                column: "TurmaId");

            migrationBuilder.CreateIndex(
                name: "IX_contaspagar_tenant_status_vencimento",
                table: "contas_pagar",
                columns: new[] { "ClinicaId", "Status", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_evolucoes_ProfissionalId",
                table: "evolucoes",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_evolucoes_ProntuarioId",
                table: "evolucoes",
                column: "ProntuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_evolucoes_tenant_prontuario_data",
                table: "evolucoes",
                columns: new[] { "ClinicaId", "ProntuarioId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_folhas_salariais_ProfissionalId",
                table: "folhas_salariais",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_folhas_tenant_profissional_competencia",
                table: "folhas_salariais",
                columns: new[] { "ClinicaId", "ProfissionalId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mensalidades_PacienteId",
                table: "mensalidades",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_mensalidades_tenant_paciente_competencia",
                table: "mensalidades",
                columns: new[] { "ClinicaId", "PacienteId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mensalidades_tenant_status",
                table: "mensalidades",
                columns: new[] { "ClinicaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_PlanoId",
                table: "pacientes",
                column: "PlanoId");

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_tenant_cpf",
                table: "pacientes",
                columns: new[] { "ClinicaId", "CPF" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_tenant_nome",
                table: "pacientes",
                columns: new[] { "ClinicaId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_planos_tenant_nome",
                table: "planos",
                columns: new[] { "ClinicaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_planos_servicos_plano_servico",
                table: "planos_servicos",
                columns: new[] { "PlanoId", "ServicoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_planos_servicos_ServicoId",
                table: "planos_servicos",
                column: "ServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_pontos_ProfissionalId",
                table: "pontos",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_pontos_tenant_profissional_data",
                table: "pontos",
                columns: new[] { "ClinicaId", "ProfissionalId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Presencas_AgendamentoId",
                table: "Presencas",
                column: "AgendamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profissionais_tenant_cpf",
                table: "profissionais",
                columns: new[] { "ClinicaId", "CPF" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prontuarios_PacienteId",
                table: "prontuarios",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_prontuarios_tenant_paciente",
                table: "prontuarios",
                columns: new[] { "ClinicaId", "PacienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_servicos_tenant_nome",
                table: "servicos",
                columns: new[] { "ClinicaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_turmas_ProfissionalId",
                table: "turmas",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_turmas_tenant_tipo",
                table: "turmas",
                columns: new[] { "ClinicaId", "TipoSessao" });

            migrationBuilder.CreateIndex(
                name: "IX_turmas_horarios_turma_dia_hora",
                table: "turmas_horarios",
                columns: new[] { "TurmaId", "DiaSemana", "HoraInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_tenant_email",
                table: "usuarios",
                columns: new[] { "ClinicaId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clinicas");

            migrationBuilder.DropTable(
                name: "contas_pagar");

            migrationBuilder.DropTable(
                name: "evolucoes");

            migrationBuilder.DropTable(
                name: "folhas_salariais");

            migrationBuilder.DropTable(
                name: "mensalidades");

            migrationBuilder.DropTable(
                name: "planos_servicos");

            migrationBuilder.DropTable(
                name: "pontos");

            migrationBuilder.DropTable(
                name: "Presencas");

            migrationBuilder.DropTable(
                name: "turmas_horarios");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "prontuarios");

            migrationBuilder.DropTable(
                name: "servicos");

            migrationBuilder.DropTable(
                name: "agendamentos");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.DropTable(
                name: "turmas");

            migrationBuilder.DropTable(
                name: "planos");

            migrationBuilder.DropTable(
                name: "profissionais");
        }
    }
}
