using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Repositorios.Migrations
{
    /// <inheritdoc />
    public partial class CorregirDescripcionesEnviosDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los datos de prueba se habían cargado con el estado del envío como
            // descripción. Se reemplazan por nombres de producto reales.
            migrationBuilder.Sql("""
                UPDATE Envios SET Descripcion = N'Caja de herramientas'
                WHERE Descripcion = N'Paquete asignado a conductor';

                UPDATE Envios SET Descripcion = N'Sobre con documentación'
                WHERE Descripcion = N'Paquete entregado';

                UPDATE Envios SET Descripcion = N'Repuestos de bicicleta'
                WHERE Descripcion = N'Paquete cancelado';

                UPDATE Envios SET Descripcion = N'Caja de libros'
                WHERE Descripcion = N'Paquete en camino';

                UPDATE Envios SET Descripcion = N'Vajilla de porcelana'
                WHERE Descripcion = N'Ejemplo: buscando conductor';

                UPDATE Envios SET Descripcion = N'Monitor de 24 pulgadas'
                WHERE Descripcion = N'Ejemplo: conductor asignado';

                UPDATE Envios SET Descripcion = N'Herramientas manuales'
                WHERE Descripcion = N'Ejemplo: retirado';

                UPDATE Envios SET Descripcion = N'Documentación de obra'
                WHERE Descripcion = N'Ejemplo: entregado';

                UPDATE Envios SET Descripcion = N'Piezas de bicicleta'
                WHERE Descripcion = N'Ejemplo: cancelado';

                UPDATE Envios SET Descripcion = N'Libros de texto'
                WHERE Descripcion = N'Ejemplo: en camino';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La corrección de datos no se revierte: los nombres de producto son válidos
            // en cualquier versión del esquema.
        }
    }
}
