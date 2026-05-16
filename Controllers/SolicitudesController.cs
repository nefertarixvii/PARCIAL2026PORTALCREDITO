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

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            string cacheKey = "mis_solicitudes_" + userId;

            var cacheData = await _cache.GetStringAsync(cacheKey);

            // ✅ CACHE
            if (!string.IsNullOrEmpty(cacheData))
            {
                var solicitudesCache =
                    JsonSerializer.Deserialize<List<SolicitudCredito>>(cacheData);

                return View(solicitudesCache ?? new List<SolicitudCredito>());
            }

            Console.WriteLine("CONSULTANDO BD...");

            var cliente = await _context.Clientes
                .Include(c => c.Solicitudes)
                .FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                return View(new List<SolicitudCredito>());
            }

            // ✅ GUARDAR CACHE
            await _cache.SetStringAsync(
                cacheKey,
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

        // 🔹 CREAR SOLICITUD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(SolicitudCredito solicitud)
        {
            // ✅ Evita error Cliente requerido
            ModelState.Remove("Cliente");

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cliente = await _context.Clientes
                .Include(c => c.Solicitudes)
                .FirstOrDefaultAsync(c => c.UsuarioId == userId);

            // ✅ CREAR CLIENTE AUTOMÁTICO
            if (cliente == null)
            {
                cliente = new Cliente
                {
                    UsuarioId = userId,
                    IngresosMensuales = 1000,
                    Activo = true,
                    Solicitudes = new List<SolicitudCredito>()
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                // 🔥 RECARGAR CLIENTE
                cliente = await _context.Clientes
                    .Include(c => c.Solicitudes)
                    .FirstOrDefaultAsync(c => c.UsuarioId == userId);
            }

            // ❌ Cliente inactivo
            if (!cliente!.Activo)
            {
                ModelState.AddModelError("", "Cliente inactivo.");
            }

            // ❌ Solo una solicitud pendiente
            if (cliente.Solicitudes.Any(s =>
                s.Estado == EstadoSolicitud.Pendiente))
            {
                ModelState.AddModelError("",
                    "Ya tienes una solicitud pendiente.");
            }

            // ❌ Monto inválido
            if (solicitud.MontoSolicitado <= 0)
            {
                ModelState.AddModelError("",
                    "El monto debe ser mayor a 0.");
            }

            // ❌ Regla 10x ingresos
            if (solicitud.MontoSolicitado >
                cliente.IngresosMensuales * 10)
            {
                ModelState.AddModelError("",
                    "El monto excede el límite permitido.");
            }

            if (!ModelState.IsValid)
            {
                return View(solicitud);
            }

            // ✅ CREAR SOLICITUD
            solicitud.ClienteId = cliente.Id;
            solicitud.FechaSolicitud = DateTime.Now;
            solicitud.Estado = EstadoSolicitud.Pendiente;

            _context.Solicitudes.Add(solicitud);

            await _context.SaveChangesAsync();

            // ✅ SESIÓN
            HttpContext.Session.SetString(
                "UltimaSolicitud",
                solicitud.MontoSolicitado.ToString());

            // ✅ INVALIDAR CACHE
            await _cache.RemoveAsync("mis_solicitudes_" + userId);

            // ✅ MENSAJE
            TempData["Success"] =
                "Solicitud registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // 🔹 DETALLE
        public async Task<IActionResult> Detalle(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }
    }
}