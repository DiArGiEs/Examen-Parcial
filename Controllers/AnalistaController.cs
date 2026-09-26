using System.Text.Json;
using Examen_Parcial.Data;
using Examen_Parcial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Examen_Parcial.Controllers
{
    [Authorize(Roles = "Analista")]
    public class AnalistaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public AnalistaController(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var solicitudesPendientes = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudesPendientes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "La solicitud especificada no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["ErrorMessage"] = "No se puede procesar una solicitud que ya ha sido Aprobada o Rechazada.";
                return RedirectToAction(nameof(Index));
            }

            var ingresos = solicitud.Cliente?.IngresosMensuales ?? 0;
            var limiteAprobacion = ingresos * 5;

            if (solicitud.MontoSolicitado > limiteAprobacion)
            {
                TempData["ErrorMessage"] = $"No se puede aprobar la solicitud #{solicitud.Id}. El monto (S/ {solicitud.MontoSolicitado:N2}) excede 5 veces los ingresos del cliente (Límite: S/ {limiteAprobacion:N2}).";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = EstadoSolicitud.Aprobado;
            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            if (solicitud.Cliente != null)
            {
                string cacheKey = $"solicitudes_user_{solicitud.Cliente.UsuarioId}";
                await _cache.RemoveAsync(cacheKey);
            }

            TempData["SuccessMessage"] = $"La solicitud #{solicitud.Id} ha sido aprobada con éxito.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivoRechazo)
        {
            var solicitud = await _context.SolicitudesCredito
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "La solicitud especificada no existe.";
                return RedirectToAction(nameof(Index));
            }

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["ErrorMessage"] = "No se puede procesar una solicitud que ya ha sido Aprobada o Rechazada.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(motivoRechazo))
            {
                TempData["ErrorMessage"] = "Debe proporcionar un motivo obligatorio para el rechazo de la solicitud.";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = EstadoSolicitud.Rechazado;
            solicitud.MotivoRechazo = motivoRechazo;
            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            if (solicitud.Cliente != null)
            {
                string cacheKey = $"solicitudes_user_{solicitud.Cliente.UsuarioId}";
                await _cache.RemoveAsync(cacheKey);
            }

            TempData["SuccessMessage"] = $"La solicitud #{solicitud.Id} ha sido rechazada.";
            return RedirectToAction(nameof(Index));
        }
    }
}