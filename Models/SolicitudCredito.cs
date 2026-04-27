using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.Models
{
    public class SolicitudCredito
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Monto debe ser mayor a 0")]
        public decimal MontoSolicitado { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        public string? MotivoRechazo { get; set; }
    }
}