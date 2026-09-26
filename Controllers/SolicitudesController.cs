using Examen_Parcial.Data;
using Examen_Parcial.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Examen_Parcial.Controllers
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

            model.Solicitudes = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
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

            return View(solicitud);
        }
    }
}