using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Repositorios.Migrations
{
    /// <inheritdoc />
    public partial class AgregarIndiceUnicoEmailUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Si la base ya tiene emails repetidos, hay que depurarlos antes de aplicar.
            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");
        }
    }
}
