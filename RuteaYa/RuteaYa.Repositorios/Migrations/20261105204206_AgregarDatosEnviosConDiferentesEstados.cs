using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Repositorios.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosEnviosConDiferentesEstados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Envios",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.Sql("""
                DECLARE @UsuarioCarlosId int = (SELECT TOP 1 Id FROM Usuarios WHERE Email = N'carlos.gonzalez@example.com');
                DECLARE @UsuarioLuciaId int = (SELECT TOP 1 Id FROM Usuarios WHERE Email = N'lucia.martinez@example.com');
                DECLARE @UsuarioAdminId int = (SELECT TOP 1 Id FROM Usuarios WHERE Email = N'admin@ruteaya.com');

                IF @UsuarioLuciaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Envios WHERE Descripcion = N'Ejemplo: buscando conductor')
                    INSERT INTO Envios (UsuarioId, OrigenDir, OrigenLat, OrigenLng, DestinoDir, DestinoLat, DestinoLng, FechaLimite, PesoKg, Tamano, Descripcion, Monto, Estado, CreadoEn)
                    VALUES (@UsuarioLuciaId, N'Suipacha 400', -34.604000, -58.379000, N'Esmeralda 800', -34.601000, -58.378000, DATEADD(day, 3, GETDATE()), 5.00, 2, N'Ejemplo: buscando conductor', 3500.00, 1, GETDATE());

                IF @UsuarioAdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Envios WHERE Descripcion = N'Ejemplo: conductor asignado')
                    INSERT INTO Envios (UsuarioId, OrigenDir, OrigenLat, OrigenLng, DestinoDir, DestinoLat, DestinoLng, FechaLimite, PesoKg, Tamano, Descripcion, Monto, Estado, CreadoEn)
                    VALUES (@UsuarioAdminId, N'Bolívar 200', -34.611000, -58.374000, N'Defensa 700', -34.615000, -58.370000, DATEADD(day, 1, GETDATE()), 1.00, 1, N'Ejemplo: conductor asignado', 1200.00, 2, GETDATE());

                IF @UsuarioCarlosId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Envios WHERE Descripcion = N'Ejemplo: retirado')
                    INSERT INTO Envios (UsuarioId, OrigenDir, OrigenLat, OrigenLng, DestinoDir, DestinoLat, DestinoLng, FechaLimite, PesoKg, Tamano, Descripcion, Monto, Estado, CreadoEn)
                    VALUES (@UsuarioCarlosId, N'Av. Belgrano 900', -34.611000, -58.377000, N'Av. Independencia 1800', -34.620000, -58.383000, DATEADD(day, 4, GETDATE()), 3.00, 1, N'Ejemplo: retirado', 2200.00, 3, GETDATE());

                IF @UsuarioCarlosId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Envios WHERE Descripcion = N'Ejemplo: entregado')
                    INSERT INTO Envios (UsuarioId, OrigenDir, OrigenLat, OrigenLng, DestinoDir, DestinoLat, DestinoLng, FechaLimite, PesoKg, Tamano, Descripcion, Monto, Estado, CreadoEn)
                    VALUES (@UsuarioCarlosId, N'Av. Córdoba 1200', -34.598000, -58.398000, N'Av. San Juan 2400', -34.627000, -58.390000, DATEADD(day, -1, GETDATE()), 1.50, 1, N'Ejemplo: entregado', 1800.00, 4, DATEADD(day, -3, GETDATE()));

                IF @UsuarioLuciaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Envios WHERE Descripcion = N'Ejemplo: cancelado')
                    INSERT INTO Envios (UsuarioId, OrigenDir, OrigenLat, OrigenLng, DestinoDir, DestinoLat, DestinoLng, FechaLimite, PesoKg, Tamano, Descripcion, Monto, Estado, CreadoEn)
                    VALUES (@UsuarioLuciaId, N'Av. Las Heras 1500', -34.588000, -58.402000, N'Av. Scalabrini Ortiz 2200', -34.583000, -58.426000, DATEADD(day, 2, GETDATE()), 4.00, 2, N'Ejemplo: cancelado', 2800.00, 5, DATEADD(day, -1, GETDATE()));

                IF @UsuarioAdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Envios WHERE Descripcion = N'Ejemplo: en camino')
                    INSERT INTO Envios (UsuarioId, OrigenDir, OrigenLat, OrigenLng, DestinoDir, DestinoLat, DestinoLng, FechaLimite, PesoKg, Tamano, Descripcion, Monto, Estado, CreadoEn)
                    VALUES (@UsuarioAdminId, N'Av. Callao 600', -34.603000, -58.395000, N'Av. Belgrano 1500', -34.613000, -58.389000, DATEADD(day, 1, GETDATE()), 2.00, 1, N'Ejemplo: en camino', 2100.00, 6, DATEADD(day, -1, GETDATE()));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM Envios
                WHERE Descripcion IN (
                    N'Ejemplo: buscando conductor',
                    N'Ejemplo: conductor asignado',
                    N'Ejemplo: retirado',
                    N'Ejemplo: entregado',
                    N'Ejemplo: cancelado',
                    N'Ejemplo: en camino'
                );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Envios",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true);
        }
    }
}
