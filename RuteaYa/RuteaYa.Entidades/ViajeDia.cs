using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RuteaYa.Entidades
{
    public class ViajeDia
    {
        [Key, Column(Order = 0)]
        public int ViajeId { get; set; }

        [ForeignKey("ViajeId")]
        public Viaje? Viaje { get; set; }

        [Key, Column(Order = 1)]
        [Required]
        [Display(Name = "Día de la Semana")]
        public byte DiaSemana { get; set; } // Mapea el tinyint de SQL
    }
}
