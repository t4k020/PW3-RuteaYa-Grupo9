using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RuteaYa.Entidades
{
    public class Envio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [Required(ErrorMessage = "La dirección de origen es obligatoria.")]
        [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
        [Display(Name = "Dirección de Origen")]
        public string OrigenDir { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Latitud de Origen")]
        public decimal OrigenLat { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Longitud de Origen")]
        public decimal OrigenLng { get; set; }

        [Required(ErrorMessage = "La dirección de destino es obligatoria.")]
        [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
        [Display(Name = "Dirección de Destino")]
        public string DestinoDir { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Latitud de Destino")]
        public decimal DestinoLat { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Longitud de Destino")]
        public decimal DestinoLng { get; set; }

        [Required(ErrorMessage = "La fecha límite es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha Límite")]
        public DateTime FechaLimite { get; set; }

        [Required(ErrorMessage = "El peso es obligatorio.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.1, 1000.0, ErrorMessage = "El peso debe ser un valor válido.")]
        [Display(Name = "Peso (Kg)")]
        public decimal PesoKg { get; set; }

        [Required]
        [Display(Name = "Tamaño")]
        public byte Tamano { get; set; } // Mapea el tipo tinyint de SQL

        [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public byte Estado { get; set; } // Mapea el tipo tinyint de SQL

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Creado En")]
        public DateTime CreadoEn { get; set; } = DateTime.Now;

        [Timestamp] // Mapea el campo rowversion / timestamp de SQL Server para control de concurrencia optimista
        public byte[]? RowVersion { get; set; }
    }
}
