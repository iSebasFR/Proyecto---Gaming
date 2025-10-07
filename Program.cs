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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();