using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RuteaYa.Entidades
{
    public class Parada
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ViajeId { get; set; }

        [ForeignKey("ViajeId")]
        public Viaje? Viaje { get; set; }

        [Required(ErrorMessage = "El orden es obligatorio.")]
        [Display(Name = "Orden")]
        public int Orden { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Latitud")]
        public decimal Lat { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Longitud")]
        public decimal Lng { get; set; }
    }
}
