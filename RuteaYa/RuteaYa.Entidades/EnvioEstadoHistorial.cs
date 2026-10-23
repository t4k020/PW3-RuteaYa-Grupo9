using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RuteaYa.Entidades
{
    public class EnvioEstadoHistorial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EnvioId { get; set; }

        [ForeignKey("EnvioId")]
        public Envio? Envio { get; set; }

        [Required]
        [Display(Name = "Estado Anterior")]
        public byte EstadoAnterior { get; set; } // Mapea el tinyint de SQL

        [Required]
        [Display(Name = "Estado Nuevo")]
        public byte EstadoNuevo { get; set; } // Mapea el tinyint de SQL

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
