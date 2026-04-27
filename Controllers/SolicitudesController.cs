using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🔹 LISTADO
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var cliente = await _context.Clientes
                .Include(c => c.Solicitudes)
                .FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
                return View(new List<SolicitudCredito>());

            return View(cliente.Solicitudes);
        }

        // 🔹 FORMULARIO
        public IActionResult Crear()
        {
            return View();
        }

        // 🔹 CREAR SOLICITUD (AQUÍ ESTÁ TODO)
        [HttpPost]
        public async Task<IActionResult> Crear(SolicitudCredito solicitud)
        {
            var userId = _userManager.GetUserId(User);

            var cliente = await _context.Clientes
                .Include(c => c.Solicitudes)
                .FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                ModelState.AddModelError("", "Cliente no encontrado");
                return View(solicitud);
            }

            // ❌ Regla 1: solo una pendiente
            if (cliente.Solicitudes.Any(s => s.Estado == EstadoSolicitud.Pendiente))
            {
                ModelState.AddModelError("", "Ya tienes una solicitud pendiente.");
            }

            // ❌ Regla 2: capacidad de pago
            if (solicitud.MontoSolicitado > cliente.IngresosMensuales * 5)
            {
                ModelState.AddModelError("", "El monto excede tu capacidad de pago.");
            }

            // ❌ Validación básica
            if (solicitud.MontoSolicitado <= 0)
            {
                ModelState.AddModelError("", "Monto inválido.");
            }

            if (!ModelState.IsValid)
                return View(solicitud);

            solicitud.ClienteId = cliente.Id;
            solicitud.FechaSolicitud = DateTime.Now;
            solicitud.Estado = EstadoSolicitud.Pendiente;

            _context.Add(solicitud);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔹 DETALLE
        public async Task<IActionResult> Detalle(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
                return NotFound();

            return View(solicitud);
        }
    }
}