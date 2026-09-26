using Examen_Parcial.Data;
using Examen_Parcial.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la cadena de conexión para SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configuración de Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.Migrate();

        if (!await roleManager.RoleExistsAsync("Analista"))
        {
            await roleManager.CreateAsync(new IdentityRole("Analista"));
        }

        var analistaEmail = "analista@banco.com";
        var analistaUser = await userManager.FindByEmailAsync(analistaEmail);
        if (analistaUser == null)
        {
            analistaUser = new IdentityUser { UserName = analistaEmail, Email = analistaEmail, EmailConfirmed = true };
            await userManager.CreateAsync(analistaUser, "Analista123!");
            await userManager.AddToRoleAsync(analistaUser, "Analista");
        }

        var cliente1Email = "cliente1@banco.com";
        var cliente1User = await userManager.FindByEmailAsync(cliente1Email);
        if (cliente1User == null)
        {
            cliente1User = new IdentityUser { UserName = cliente1Email, Email = cliente1Email, EmailConfirmed = true };
            await userManager.CreateAsync(cliente1User, "Cliente123!");
        }

        var cliente2Email = "cliente2@banco.com";
        var cliente2User = await userManager.FindByEmailAsync(cliente2Email);
        if (cliente2User == null)
        {
            cliente2User = new IdentityUser { UserName = cliente2Email, Email = cliente2Email, EmailConfirmed = true };
            await userManager.CreateAsync(cliente2User, "Cliente123!");
        }

        if (!context.Clientes.Any())
        {
            var cliente1 = new Cliente { UsuarioId = cliente1User.Id, IngresosMensuales = 3000, Activo = true };
            var cliente2 = new Cliente { UsuarioId = cliente2User.Id, IngresosMensuales = 5000, Activo = true };

            context.Clientes.AddRange(cliente1, cliente2);
            await context.SaveChangesAsync();

            var solicitud1 = new SolicitudCredito
            {
                ClienteId = cliente1.Id,
                MontoSolicitado = 5000,
                FechaSolicitud = DateTime.Now.AddDays(-2),
                Estado = EstadoSolicitud.Pendiente
            };

            var solicitud2 = new SolicitudCredito
            {
                ClienteId = cliente2.Id,
                MontoSolicitado = 10000,
                FechaSolicitud = DateTime.Now.AddDays(-5),
                Estado = EstadoSolicitud.Aprobado
            };

            context.SolicitudesCredito.AddRange(solicitud1, solicitud2);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al cargar la data inicial.");
    }
}

app.Run();