using System.ComponentModel.DataAnnotations;

namespace Examen_Parcial.Models
{
    public class SolicitudCrearViewModel
    {
        [Required(ErrorMessage = "El monto solicitado es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto solicitado debe ser mayor a 0.")]
        [Display(Name = "Monto Solicitado (S/)")]
        public decimal MontoSolicitado { get; set; }

        public decimal IngresosMensuales { get; set; }
        public decimal MontoMaximoPermitido => IngresosMensuales * 10;
    }
}