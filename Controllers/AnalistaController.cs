using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers
{
    [Authorize(Roles = "Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalistaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pendientes = await _context.Solicitudes
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .ToListAsync();

            return View(pendientes);
        }

        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
                return BadRequest("Solicitud inválida");

            if (solicitud.MontoSolicitado > solicitud.Cliente.IngresosMensuales * 5)
                return BadRequest("No cumple capacidad de pago");

            solicitud.Estado = EstadoSolicitud.Aprobado;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar(int id, string motivo)
        {
            var solicitud = await _context.Solicitudes.FindAsync(id);

            if (solicitud == null || solicitud.Estado != EstadoSolicitud.Pendiente)
                return BadRequest("Solicitud inválida");

            if (string.IsNullOrEmpty(motivo))
                return BadRequest("Motivo obligatorio");

            solicitud.Estado = EstadoSolicitud.Rechazado;
            solicitud.MotivoRechazo = motivo;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}