using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ✅ AGREGAR CACHÉ EN MEMORIA
builder.Services.AddMemoryCache();

// Configurar PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ CONFIGURAR RAWG SERVICE
builder.Services.AddHttpClient<IRawgService, RawgService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IRawgService, RawgService>();

// ✅ CONFIGURAR IDENTITY CORRECTAMENTE
builder.Services.AddDefaultIdentity<Usuario>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()   
.AddEntityFrameworkStores<ApplicationDbContext>();

// ✅ SESSIONS (opcional pero recomendado)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// ✅ PRECARGAR DATOS AL INICIAR (OPCIONAL)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var rawgService = scope.ServiceProvider.GetRequiredService<IRawgService>();

        // Ejecutar en segundo plano sin esperar
        _ = Task.Run(async () =>
        {
            try
            {
                await rawgService.PreloadFirst100GamesAsync();
                Console.WriteLine("Precarga de juegos completada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en precarga: {ex.Message}");
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"No se pudo iniciar la precarga: {ex.Message}");
    }
}

    // ✅ SEMILLA: rol Admin + usuario admin (seguro, idempotente)
try
{
    using var scope = app.Services.CreateScope();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

    // 1) Crear rol "Admin" si no existe
    if (!await roleMgr.RoleExistsAsync("Admin"))
        await roleMgr.CreateAsync(new IdentityRole("Admin"));

    // 2) Crear usuario admin inicial (ajusta correo/clave si quieres)
    var adminEmail = "admin@gaming.com";
    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new Usuario
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrador"
        };

        // Nota: la contraseña cumple tus reglas actuales
        var createResult = await userMgr.CreateAsync(admin, "Admin#2025!");
        if (createResult.Succeeded)
        {
            await userMgr.AddToRoleAsync(admin, "Admin");
            Console.WriteLine("Usuario admin creado y asignado al rol Admin.");
        }
        else
        {
            Console.WriteLine("No se pudo crear el usuario admin: " +
                string.Join(" | ", createResult.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        // Asegurar que tiene el rol
        if (!await userMgr.IsInRoleAsync(admin, "Admin"))
            await userMgr.AddToRoleAsync(admin, "Admin");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Seed Admin] Error: {ex.Message}");
}


// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ✅ ESTOS MIDDLEWARES EN ORDEN CORRECTO
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ✅ MAPEOS NECESARIOS PARA IDENTITY
app.MapControllers();
app.MapRazorPages();  // ← ESTO ES CRÍTICO PARA LOGIN/REGISTER

// ✅ Ruta para las ÁREAS (como Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


