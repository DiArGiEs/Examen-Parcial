using System.ComponentModel.DataAnnotations;

namespace Examen_Parcial.Models
{
    public class SolicitudFiltroViewModel
    {
        public EstadoSolicitud? Estado { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto mínimo no puede ser negativo.")]
        public decimal? MontoMin { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto máximo no puede ser negativo.")]
        public decimal? MontoMax { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaInicio { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaFin { get; set; }

        public List<SolicitudCredito> Solicitudes { get; set; } = new List<SolicitudCredito>();
    }
}