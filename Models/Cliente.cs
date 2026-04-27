using System.ComponentModel.DataAnnotations;

namespace PlataformaCreditos.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        public string UsuarioId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ingresos debe ser mayor a 0")]
        public decimal IngresosMensuales { get; set; }

        public bool Activo { get; set; } = true;

        public List<SolicitudCredito> Solicitudes { get; set; } = new();
    }
}