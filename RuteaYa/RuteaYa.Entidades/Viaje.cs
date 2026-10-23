using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RuteaYa.Entidades
{
    public class Viaje
    {
        [Key]
        public int Id { get; set; } // PK[cite: 3]

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public int UsuarioId { get; set; } // FK[cite: 3]

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [Required(ErrorMessage = "La dirección de origen es obligatoria.")]
        [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
        [Display(Name = "Dirección de Origen")]
        public string OrigenDir { get; set; } = string.Empty; // string[cite: 3]

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Latitud de Origen")]
        public decimal OrigenLat { get; set; } // decimal[cite: 3]

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Longitud de Origen")]
        public decimal OrigenLng { get; set; } // decimal[cite: 3]

        [Required(ErrorMessage = "La dirección de destino es obligatoria.")]
        [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
        [Display(Name = "Dirección de Destino")]
        public string DestinoDir { get; set; } = string.Empty; // string[cite: 3]

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Latitud de Destino")]
        public decimal DestinoLat { get; set; } // decimal[cite: 3]

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        [Display(Name = "Longitud de Destino")]
        public decimal DestinoLng { get; set; } // decimal[cite: 3]

        [Required(ErrorMessage = "La hora de salida es obligatoria.")]
        [Display(Name = "Hora de Salida")]
        public TimeOnly HoraSalida { get; set; } // time[cite: 3]

        [Required(ErrorMessage = "La hora de llegada es obligatoria.")]
        [Display(Name = "Hora de Llegada")]
        public TimeOnly HoraLlegada { get; set; } // time[cite: 3]

        [Required(ErrorMessage = "El máximo de paquetes es obligatorio.")]
        [Range(1, 1000, ErrorMessage = "Debe ser un valor válido.")]
        [Display(Name = "Máximo de Paquetes")]
        public int MaxPaquetes { get; set; } // int[cite: 3]

        [Required(ErrorMessage = "El peso máximo es obligatorio.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Peso Máximo (Kg)")]
        public decimal PesoMaxKg { get; set; } // decimal[cite: 3]

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Desvío Máximo (Km)")]
        public decimal DesvioMaxKm { get; set; } // decimal[cite: 3]

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Distancia Total (Km)")]
        public decimal DistanciaKm { get; set; } // decimal[cite: 3]

        [Required]
        [Display(Name = "Duración (Minutos)")]
        public int DuracionMin { get; set; } // int[cite: 3]

        [Required]
        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true; // bool[cite: 3]

        // Colecciones de navegación para las relaciones
        public ICollection<Parada> Paradas { get; set; } = new List<Parada>();
        public ICollection<ViajeDia> ViajeDias { get; set; } = new List<ViajeDia>();
        public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
    }
}
