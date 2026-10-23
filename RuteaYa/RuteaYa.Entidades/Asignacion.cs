using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RuteaYa.Entidades
{
    public class Asignacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EnvioId { get; set; }

        [ForeignKey("EnvioId")]
        public Envio? Envio { get; set; }

        [Required]
        public int ViajeId { get; set; }

        [ForeignKey("ViajeId")]
        public Viaje? Viaje { get; set; }

        [Required(ErrorMessage = "La fecha de viaje es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Viaje")]
        public DateTime FechaViaje { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Desvío (Km)")]
        public decimal DesvioKm { get; set; }

        [Required]
        [Display(Name = "Tiempo Extra (Min)")]
        public int TiempoExtraMin { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Ganancia")]
        public decimal Ganancia { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Asignado En")]
        public DateTime AsignadoEn { get; set; } = DateTime.Now;
    }
}
