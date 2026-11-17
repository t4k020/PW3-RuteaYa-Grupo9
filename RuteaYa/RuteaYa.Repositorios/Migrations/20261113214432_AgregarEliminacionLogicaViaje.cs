using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Repositorios.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEliminacionLogicaViaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EliminadoEn",
                table: "Viajes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EliminadoEn",
                table: "Viajes");
        }
    }
}
