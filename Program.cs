using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ✅ CONFIGURAR SERVICIO DE PAGOS STRIPE CON HTTPCLIENT
builder.Services.AddHttpClient<IPaymentService, StripePaymentService>();

// ✅ CONFIGURAR REDIS (DISTRIBUTED CACHE)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "Gaming_";
});

// ✅ SERVICIO DE MANTENIMIENTO AUTOMÁTICO
builder.Services.AddHostedService<CacheMaintenanceService>();

// ✅ CONFIGURAR SESIONES DISTRIBUIDAS
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Gaming.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ✅ MANTENER CACHÉ EN MEMORIA PARA DATOS LOCALES
builder.Services.AddMemoryCache();

// Configurar PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ CONFIGURAR RAWG SERVICE
builder.Services.AddHttpClient<IRawgService, RawgService>(client =>
{
    client.BaseAddress = new Uri("https://api.rawg.io/api/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "GamingApp/1.0");
});

// ✅ STATISTICS SERVICE
builder.Services.AddScoped<IStatsService, StatsService>();

// ✅ OBTENER CREDENCIALES DE GOOGLE (User Secrets tiene prioridad)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

// ✅ LOGS INFORMATIVOS PARA EL EQUIPO
Console.WriteLine("🔐 Configuración Google Auth:");
Console.WriteLine($"   - ClientId: {!string.IsNullOrEmpty(googleClientId)}");
Console.WriteLine($"   - ClientSecret: {!string.IsNullOrEmpty(googleClientSecret)}");

if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
{
    Console.WriteLine("⚠️  INSTRUCCIONES PARA EL EQUIPO:");
    Console.WriteLine("   Ejecutar estos comandos en la raíz del proyecto:");
    Console.WriteLine("   dotnet user-secrets init");
    Console.WriteLine("   dotnet user-secrets set \"Authentication:Google:ClientId\" \"TU_CLIENT_ID\"");
    Console.WriteLine("   dotnet user-secrets set \"Authentication:Google:ClientSecret\" \"TU_CLIENT_SECRET\"");
}

// ✅ CONFIGURAR IDENTITY
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
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ✅ CONFIGURACIÓN DE COOKIES GENERAL
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
});

// ✅ CONFIGURACIÓN DE COOKIES EXTERNAS (para proveedores como Google)
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ✅ CONFIGURAR GOOGLE AUTH SOLO SI HAY CREDENCIALES
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/signin-google";
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.SaveTokens = true;
            
            // ✅ CORRELATION COOKIE SE CONFIGURA DENTRO DE GOOGLE OPTIONS
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        });
    
    Console.WriteLine("✅ Google Authentication configurado correctamente");
}
else
{
    Console.WriteLine("⏸️  Google Authentication pendiente - Configurar User Secrets");
}

// ✅ REGISTRAR SERVICIO DE GOOGLE AUTH
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();

// ✅ Email service (FileMode o SMTP según configuración)
var emailMode = builder.Configuration["Email:Mode"] ?? "File";
if (emailMode.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, FileEmailService>();
}

// ✅ Servicio de precios de juegos
builder.Services.AddHttpClient<IGamePriceService, CheapSharkPriceService>();
builder.Services.AddScoped<IGamePriceService, CheapSharkPriceService>();

var app = builder.Build();

// ✅ INICIALIZACIÓN AUTOMÁTICA AL INICIAR (REEMPLAZA LA PRECARGA DUPLICADA)
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("🚀 Inicializando sistema de caché automático...");
    
    // Ejecutar en segundo plano sin bloquear
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var rawgService = scope.ServiceProvider.GetRequiredService<IRawgService>();
            var redisCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            
            // 1. Limpiar caché corrupto al iniciar
            await ClearCorruptedCacheOnStartup(redisCache);
            
            // 2. Precargar datos esenciales
            await PreloadEssentialData(rawgService, redisCache);
            
            Console.WriteLine("✅ Sistema de caché automático inicializado");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error en inicialización automática: {ex.Message}");
        }
    });
});

// MÉTODOS DE INICIALIZACIÓN AUTOMÁTICA
async Task ClearCorruptedCacheOnStartup(IDistributedCache redisCache)
{
    try
    {
        // Limpiar solo las claves que sabemos que se corrompen
        for (int i = 1; i <= 5; i++)
        {
            await redisCache.RemoveAsync($"Games____{i}");
        }
        Console.WriteLine("🧹 Caché corrupto limpiado al iniciar");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ No se pudo limpiar caché al iniciar: {ex.Message}");
    }
}

async Task PreloadEssentialData(IRawgService rawgService, IDistributedCache redisCache)
{
    try
    {
        Console.WriteLine("📦 Precargando datos esenciales...");
        
        // Precargar solo página 1 y filtros (más rápido y confiable)
        var page1Task = rawgService.GetGamesAsync("", "", "", 1);
        var filtersTask = rawgService.GetAvailableFiltersAsync();
        
        await Task.WhenAll(page1Task, filtersTask);
        
        Console.WriteLine($"✅ Precarga automática - Juegos: {page1Task.Result.Results?.Count ?? 0}, Filtros: {filtersTask.Result.AvailableGenres?.Count ?? 0}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Precarga automática falló: {ex.Message}");
    }
}

// ✅ SEMILLA: rol Admin + usuario admin
try
{
    using var scope = app.Services.CreateScope();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

    if (!await roleMgr.RoleExistsAsync("Admin"))
    {
        await roleMgr.CreateAsync(new IdentityRole("Admin"));
        Console.WriteLine("👑 Rol Admin creado");
    }

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

        var createResult = await userMgr.CreateAsync(admin, "Admin#2025!");
        if (createResult.Succeeded)
        {
            await userMgr.AddToRoleAsync(admin, "Admin");
            Console.WriteLine("✅ Usuario admin creado y asignado al rol Admin");
        }
        else
        {
            Console.WriteLine("❌ No se pudo crear el usuario admin: " +
                string.Join(" | ", createResult.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        if (!await userMgr.IsInRoleAsync(admin, "Admin"))
        {
            await userMgr.AddToRoleAsync(admin, "Admin");
            Console.WriteLine("✅ Rol Admin asignado al usuario existente");
        }
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

// ✅ ORDEN CORRECTO DE MIDDLEWARES (SESSION ANTES DE AUTH)
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ✅ MAPEOS NECESARIOS PARA IDENTITY
app.MapControllers();
app.MapRazorPages();

// ✅ Ruta para las ÁREAS
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();