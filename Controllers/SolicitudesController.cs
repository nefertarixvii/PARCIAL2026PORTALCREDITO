using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PlataformaCreditos.Data;
using PlataformaCreditos.Models;
using System.Text.Json;

namespace PlataformaCreditos.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _cache;

        public SolicitudesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // 🔹 LISTADO CON CACHE
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            string cacheKey = "mis_solicitudes_" + userId;

            var cacheData = await _cache.GetStringAsync(cacheKey);

            if (cacheData != null)
            {
                var solicitudesCache = JsonSerializer.Deserialize<List<SolicitudCredito>>(cacheData);
                return View(solicitudesCache);
            }

            var cliente = await _context.Clientes
                .Include(c => c.Solicitudes)
                .FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
                return View(new List<SolicitudCredito>());

            await _cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(cliente.Solicitudes),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                });

            return View(cliente.Solicitudes);
        }

        // 🔹 FORMULARIO
        public IActionResult Crear()
        {
            return View();
        }

        // 🔹 CREAR
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

            // ❌ Cliente activo
            if (!cliente.Activo)
            {
                ModelState.AddModelError("", "Cliente inactivo");
            }

            // ❌ Solo una pendiente
            if (cliente.Solicitudes.Any(s => s.Estado == EstadoSolicitud.Pendiente))
            {
                ModelState.AddModelError("", "Ya tienes una solicitud pendiente.");
            }

            // ❌ Regla 10x ingresos
            if (solicitud.MontoSolicitado > cliente.IngresosMensuales * 10)
            {
                ModelState.AddModelError("", "El monto excede tu capacidad de pago.");
            }

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

            // ✅ Sesión
            HttpContext.Session.SetString("UltimaSolicitud", solicitud.MontoSolicitado.ToString());

            // ✅ Cache invalidar
            await _cache.RemoveAsync("mis_solicitudes_" + userId);

            // ✅ Mensaje éxito
            TempData["Success"] = "Solicitud registrada correctamente";

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