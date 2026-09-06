using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanchasApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sede",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sede", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoCancha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PrecioPorHora = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoCancha", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.CheckConstraint("CK_Usuario_Rol", "([Rol]='Cliente' OR [Rol]='Trabajador' OR [Rol]='Administrador')");
                });

            migrationBuilder.CreateTable(
                name: "Cancha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    SedeId = table.Column<int>(type: "int", nullable: false),
                    TipoCanchaId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cancha", x => x.Id);
                    table.CheckConstraint("CK_Cancha_Estado", "([Estado]='Fuera de servicio' OR [Estado]='Mantenimiento' OR [Estado]='Disponible')");
                    table.ForeignKey(
                        name: "FK_Cancha_Sede",
                        column: x => x.SedeId,
                        principalTable: "Sede",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cancha_TipoCancha",
                        column: x => x.TipoCanchaId,
                        principalTable: "TipoCancha",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Leido = table.Column<bool>(type: "bit", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacion_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trabajador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trabajador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trabajador_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Horario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanchaId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: false),
                    HoraApertura = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraCierre = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horario", x => x.Id);
                    table.CheckConstraint("CK_Horario_Dia", "([DiaSemana]>=(1) AND [DiaSemana]<=(7))");
                    table.CheckConstraint("CK_Horario_Rango", "([HoraCierre]>[HoraApertura])");
                    table.ForeignKey(
                        name: "FK_Horario_Cancha",
                        column: x => x.CanchaId,
                        principalTable: "Cancha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanchaId = table.Column<int>(type: "int", nullable: false),
                    TrabajadorId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mantenimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mantenimiento_Cancha",
                        column: x => x.CanchaId,
                        principalTable: "Cancha",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mantenimiento_Trabajador",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajador",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Reserva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CanchaId = table.Column<int>(type: "int", nullable: false),
                    TrabajadorId = table.Column<int>(type: "int", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reserva", x => x.Id);
                    table.CheckConstraint("CK_Reserva_Estado", "([Estado]='Completada' OR [Estado]='Cancelada' OR [Estado]='Confirmada' OR [Estado]='Pendiente')");
                    table.CheckConstraint("CK_Reserva_Fechas", "([FechaFin]>[FechaInicio])");
                    table.ForeignKey(
                        name: "FK_Reserva_Cancha",
                        column: x => x.CanchaId,
                        principalTable: "Cancha",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reserva_Trabajador",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajador",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reserva_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.Id);
                    table.CheckConstraint("CK_Pago_Estado", "([Estado]='Reembolsado' OR [Estado]='Pagado' OR [Estado]='Pendiente')");
                    table.CheckConstraint("CK_Pago_Metodo", "([MetodoPago]='Transferencia' OR [MetodoPago]='Tarjeta' OR [MetodoPago]='Efectivo')");
                    table.ForeignKey(
                        name: "FK_Pago_Reserva",
                        column: x => x.ReservaId,
                        principalTable: "Reserva",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Resena",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Calificacion = table.Column<byte>(type: "tinyint", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resena", x => x.Id);
                    table.CheckConstraint("CK_Resena_Calificacion", "([Calificacion]>=(1) AND [Calificacion]<=(5))");
                    table.ForeignKey(
                        name: "FK_Resena_Reserva",
                        column: x => x.ReservaId,
                        principalTable: "Reserva",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resena_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Comprobante",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PagoId = table.Column<int>(type: "int", nullable: false),
                    NumeroComprobante = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobante", x => x.Id);
                    table.CheckConstraint("CK_Comprobante_Tipo", "([Tipo]='Factura' OR [Tipo]='Boleta')");
                    table.ForeignKey(
                        name: "FK_Comprobante_Pago",
                        column: x => x.PagoId,
                        principalTable: "Pago",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cancha_SedeId",
                table: "Cancha",
                column: "SedeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cancha_TipoCanchaId",
                table: "Cancha",
                column: "TipoCanchaId");

            migrationBuilder.CreateIndex(
                name: "UQ_Comprobante_PagoId",
                table: "Comprobante",
                column: "PagoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Horario_CanchaId",
                table: "Horario",
                column: "CanchaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimiento_CanchaId",
                table: "Mantenimiento",
                column: "CanchaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimiento_TrabajadorId",
                table: "Mantenimiento",
                column: "TrabajadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_UsuarioId",
                table: "Notificacion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_ReservaId",
                table: "Pago",
                column: "ReservaId");

            migrationBuilder.CreateIndex(
                name: "IX_Resena_UsuarioId",
                table: "Resena",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UQ_Resena_ReservaId",
                table: "Resena",
                column: "ReservaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_CanchaId",
                table: "Reserva",
                column: "CanchaId");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_TrabajadorId",
                table: "Reserva",
                column: "TrabajadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_UsuarioId",
                table: "Reserva",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UQ_Trabajador_UsuarioId",
                table: "Trabajador",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comprobante");

            migrationBuilder.DropTable(
                name: "Horario");

            migrationBuilder.DropTable(
                name: "Mantenimiento");

            migrationBuilder.DropTable(
                name: "Notificacion");

            migrationBuilder.DropTable(
                name: "Resena");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "Reserva");

            migrationBuilder.DropTable(
                name: "Cancha");

            migrationBuilder.DropTable(
                name: "Trabajador");

            migrationBuilder.DropTable(
                name: "Sede");

            migrationBuilder.DropTable(
                name: "TipoCancha");

            migrationBuilder.DropTable(
                name: "Usuario");
        }
    }
}
