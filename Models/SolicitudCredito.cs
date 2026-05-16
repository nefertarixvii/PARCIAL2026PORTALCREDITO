using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace PlataformaCreditos.Models
{
    public class SolicitudCredito
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }

        // 🔥 IMPORTANTE
        [BindNever]
        [JsonIgnore]
        public Cliente? Cliente { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, double.MaxValue,
            ErrorMessage = "Monto debe ser mayor a 0")]
        public decimal MontoSolicitado { get; set; }

        public DateTime FechaSolicitud { get; set; }
            = DateTime.Now;

        public EstadoSolicitud Estado { get; set; }
            = EstadoSolicitud.Pendiente;

        public string? MotivoRechazo { get; set; }
    }
}