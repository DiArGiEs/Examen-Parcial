using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Examen_Parcial.Models
{
    public enum EstadoSolicitud
    {
        Pendiente,
        Aprobado,
        Rechazado
    }

    public class SolicitudCredito
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor a 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoSolicitado { get; set; }

        [Required]
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Required]
        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        public string? MotivoRechazo { get; set; }
    }
}