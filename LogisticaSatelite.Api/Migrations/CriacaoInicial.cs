using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticaSatelite.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Destinatario = table.Column<string>(type: "TEXT", nullable: false),
                    Modal = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Volumes = table.Column<int>(type: "INTEGER", nullable: false),
                    Peso = table.Column<double>(type: "REAL", nullable: false),
                    SepUser = table.Column<string>(type: "TEXT", nullable: true),
                    ConfUser = table.Column<string>(type: "TEXT", nullable: true),
                    ExpUser = table.Column<string>(type: "TEXT", nullable: true),
                    SepData = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfData = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpData = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LockedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    User = table.Column<string>(type: "TEXT", nullable: false),
                    Senha = table.Column<string>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Perfil = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensPedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    nome = table.Column<string>(type: "TEXT", nullable: false),
                    descricao = table.Column<string>(type: "TEXT", nullable: false),
                    CodItem = table.Column<string>(type: "TEXT", nullable: false),
                    LocalRua = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPrateleira = table.Column<string>(type: "TEXT", nullable: false),
                    LocalCompartimento = table.Column<string>(type: "TEXT", nullable: false),
                    LocalAndar = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalArea = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPalet = table.Column<string>(type: "TEXT", nullable: false),
                    UnidadepCx = table.Column<int>(type: "INTEGER", nullable: false),
                    ScansSep = table.Column<int>(type: "INTEGER", nullable: false),
                    ScansConf = table.Column<int>(type: "INTEGER", nullable: false),
                    ScansExp = table.Column<int>(type: "INTEGER", nullable: false),
                    NeedAbaste = table.Column<bool>(type: "INTEGER", nullable: false),
                    Abastecido = table.Column<bool>(type: "INTEGER", nullable: false),
                    QtdCxAbaste = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensPedidos_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedidos_PedidoId",
                table: "ItensPedidos",
                column: "PedidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensPedidos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Pedidos");
        }
    }
}
