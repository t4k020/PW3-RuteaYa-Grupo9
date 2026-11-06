using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Repositorios.Migrations
{
    /// <inheritdoc />
    public partial class EliminarEstadoPendienteEnvio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Envios
                SET Estado = 1
                WHERE Estado = 0;

                UPDATE EnvioEstadoHistoriales
                SET EstadoAnterior = 1
                WHERE EstadoAnterior = 0;

                UPDATE EnvioEstadoHistoriales
                SET EstadoNuevo = 1
                WHERE EstadoNuevo = 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
