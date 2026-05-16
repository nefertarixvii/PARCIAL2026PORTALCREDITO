using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers
{
    [Authorize(Roles = "Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public AnalistaController(
            ApplicationDbContext context,
            IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // 🔹 LISTAR PENDIENTES
        public async Task<IActionResult> Index()
        {
            var pendientes = await _context.Solicitudes
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .ToListAsync();

            return View(pendientes);
        }

        // 🔹 APROBAR
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            // ❌ Solicitud inválida
            if (solicitud == null)
            {
                TempData["Error"] = "Solicitud no encontrada";
                return RedirectToAction(nameof(Index));
            }

            // ❌ Ya procesada
            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] =
                    "La solicitud ya fue procesada";

                return RedirectToAction(nameof(Index));
            }

            // ❌ Regla 5x ingresos
            if (solicitud.MontoSolicitado >
                solicitud.Cliente!.IngresosMensuales * 5)
            {
                TempData["Error"] =
                    "No cumple capacidad de pago";

                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = EstadoSolicitud.Aprobado;

            await _context.SaveChangesAsync();

            // ✅ Invalidar cache
            await _cache.RemoveAsync(
                "mis_solicitudes_" + solicitud.Cliente.UsuarioId);

            TempData["Success"] =
                "Solicitud aprobada correctamente";

            return RedirectToAction(nameof(Index));
        }

        // 🔹 FORM RECHAZAR
        public async Task<IActionResult> Rechazar(int id)
        {
            var solicitud = await _context.Solicitudes
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }

        // 🔹 RECHAZAR POST
        [HttpPost]
        public async Task<IActionResult> Rechazar(
            int id,
            string motivo)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            // ❌ inválida
            if (solicitud == null)
            {
                TempData["Error"] = "Solicitud inválida";
                return RedirectToAction(nameof(Index));
            }

            // ❌ Ya procesada
            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] =
                    "La solicitud ya fue procesada";

                return RedirectToAction(nameof(Index));
            }

            // ❌ Motivo obligatorio
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] =
                    "Motivo obligatorio";

                return View(solicitud);
            }

            solicitud.Estado = EstadoSolicitud.Rechazado;
            solicitud.MotivoRechazo = motivo;

            await _context.SaveChangesAsync();

            // ✅ invalidar cache
            await _cache.RemoveAsync(
                "mis_solicitudes_" + solicitud.Cliente!.UsuarioId);

            TempData["Success"] =
                "Solicitud rechazada correctamente";

            return RedirectToAction(nameof(Index));
        }
    }
}