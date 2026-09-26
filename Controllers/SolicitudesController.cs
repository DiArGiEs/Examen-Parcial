using System.Text.Json;
using Examen_Parcial.Data;
using Examen_Parcial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Examen_Parcial.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _cache;

        public SolicitudesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        public async Task<IActionResult> MisSolicitudes(SolicitudFiltroViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                model.Solicitudes = new List<SolicitudCredito>();
                return View(model);
            }

            if (model.MontoMin.HasValue && model.MontoMin < 0)
            {
                ModelState.AddModelError("MontoMin", "El monto mínimo no puede ser negativo.");
            }

            if (model.MontoMax.HasValue && model.MontoMax < 0)
            {
                ModelState.AddModelError("MontoMax", "El monto máximo no puede ser negativo.");
            }

            if (model.FechaInicio.HasValue && model.FechaFin.HasValue && model.FechaInicio > model.FechaFin)
            {
                ModelState.AddModelError("FechaInicio", "La fecha inicio no puede ser mayor que la fecha fin.");
            }

            List<SolicitudCredito>? solicitudesList = null;
            string cacheKey = $"solicitudes_user_{userId}";

            bool tieneFiltros = model.Estado.HasValue || model.MontoMin.HasValue || model.MontoMax.HasValue || model.FechaInicio.HasValue || model.FechaFin.HasValue;

            if (!tieneFiltros && ModelState.IsValid)
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    solicitudesList = JsonSerializer.Deserialize<List<SolicitudCredito>>(cachedData);
                }
            }

            if (solicitudesList == null)
            {
                var query = _context.SolicitudesCredito
                    .Include(s => s.Cliente)
                    .Where(s => s.ClienteId == cliente.Id)
                    .AsQueryable();

                if (ModelState.IsValid)
                {
                    if (model.Estado.HasValue)
                    {
                        query = query.Where(s => s.Estado == model.Estado.Value);
                    }

                    if (model.MontoMin.HasValue)
                    {
                        query = query.Where(s => s.MontoSolicitado >= model.MontoMin.Value);
                    }

                    if (model.MontoMax.HasValue)
                    {
                        query = query.Where(s => s.MontoSolicitado <= model.MontoMax.Value);
                    }

                    if (model.FechaInicio.HasValue)
                    {
                        query = query.Where(s => s.FechaSolicitud >= model.FechaInicio.Value);
                    }

                    if (model.FechaFin.HasValue)
                    {
                        var fechaFinFinDeDia = model.FechaFin.Value.Date.AddDays(1).AddTicks(-1);
                        query = query.Where(s => s.FechaSolicitud <= fechaFinFinDeDia);
                    }
                }

                solicitudesList = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

                if (!tieneFiltros && ModelState.IsValid)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                    };
                    var options = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(solicitudesList, options), cacheOptions);
                }
            }

            model.Solicitudes = solicitudesList;
            return View(model);
        }

        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            if (cliente != null && solicitud.ClienteId != cliente.Id && !User.IsInRole("Analista"))
            {
                return Forbid();
            }

            HttpContext.Session.SetInt32("UltimaSolicitudId", solicitud.Id);
            HttpContext.Session.SetString("UltimaSolicitudMonto", solicitud.MontoSolicitado.ToString("N2"));

            return View(solicitud);
        }

        public async Task<IActionResult> Crear()
        {
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Usted no está registrado como cliente del sistema.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            var model = new SolicitudCrearViewModel
            {
                IngresosMensuales = cliente.IngresosMensuales
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(SolicitudCrearViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                TempData["ErrorMessage"] = "Usted no está registrado como cliente del sistema.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            model.IngresosMensuales = cliente.IngresosMensuales;

            if (!cliente.Activo)
            {
                ModelState.AddModelError(string.Empty, "Su cuenta de cliente se encuentra inactiva. No puede registrar nuevas solicitudes.");
            }

            var tienePendiente = await _context.SolicitudesCredito
                .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

            if (tienePendiente)
            {
                ModelState.AddModelError(string.Empty, "Ya posee una solicitud de crédito activa en estado Pendiente.");
            }

            if (model.MontoSolicitado > (cliente.IngresosMensuales * 10))
            {
                ModelState.AddModelError("MontoSolicitado", $"El monto solicitado no puede exceder 10 veces sus ingresos mensuales (Máximo permitido: S/ {cliente.IngresosMensuales * 10:N2}).");
            }

            if (ModelState.IsValid)
            {
                var nuevaSolicitud = new SolicitudCredito
                {
                    ClienteId = cliente.Id,
                    MontoSolicitado = model.MontoSolicitado,
                    FechaSolicitud = DateTime.Now,
                    Estado = EstadoSolicitud.Pendiente
                };

                _context.Add(nuevaSolicitud);
                await _context.SaveChangesAsync();

                string cacheKey = $"solicitudes_user_{userId}";
                await _cache.RemoveAsync(cacheKey);

                TempData["SuccessMessage"] = "Solicitud de crédito registrada exitosamente con estado Pendiente.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            return View(model);
        }
    }
}