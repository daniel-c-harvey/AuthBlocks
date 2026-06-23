using System.Runtime.ExceptionServices;
using AuthBlocksLib;
using AuthBlocksLib.Options;
using AuthBlocksWeb.TestHost.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// DIAGNOSTIC: first-chance exception capture.
//
// The bug under investigation is an ArgumentNullException (Parameter 'source')
// that is caught deep in the AuthBlocks request pipeline and reshaped into a
// failed NetBlocks.Result — so it never surfaces a stack trace. A first-chance
// handler logs the FULL stack at the throw site, BEFORE any catch swallows it.
// Scoped to ArgumentNullException to keep the log readable. This lives only in
// the test host; no AuthBlocks library code is touched.
// ---------------------------------------------------------------------------
AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
{
    if (e.Exception is ArgumentNullException ane)
    {
        Console.Error.WriteLine("=== FIRST-CHANCE ArgumentNullException ===");
        Console.Error.WriteLine(ane.ToString());
        Console.Error.WriteLine("=== END FIRST-CHANCE ===");
    }
};

builder.Services.AddMudServices();

// --- AuthBlocks API host wiring (mirrors SkipperAPI / DeepDrftAPI) ----------
// Local Postgres; UseAuthBlocksStartupAsync auto-applies migrations and seeds
// the placeholder admin on first boot.
builder.Services.AddAuthBlocks(opts =>
{
    opts.ConnectionString = builder.Configuration.GetConnectionString("Auth")
        ?? throw new InvalidOperationException("ConnectionStrings:Auth is required");
    opts.ApplicationName = "AuthBlocks Test Host";
    opts.SupportEmail = builder.Configuration["AuthBlocks:SupportEmail"] ?? "placeholder@example.invalid";

    opts.JwtSettings.Secret = builder.Configuration["AuthBlocks:Jwt:Secret"]
        ?? throw new InvalidOperationException("AuthBlocks:Jwt:Secret is required");
    opts.JwtSettings.Issuer = builder.Configuration["AuthBlocks:Jwt:Issuer"]
        ?? throw new InvalidOperationException("AuthBlocks:Jwt:Issuer is required");
    opts.JwtSettings.Audience = builder.Configuration["AuthBlocks:Jwt:Audience"]
        ?? throw new InvalidOperationException("AuthBlocks:Jwt:Audience is required");

    opts.EmailConnection.Host = builder.Configuration["AuthBlocks:Email:Host"]
        ?? throw new InvalidOperationException("AuthBlocks:Email:Host is required");
    opts.EmailConnection.Token = builder.Configuration["AuthBlocks:Email:Token"]
        ?? throw new InvalidOperationException("AuthBlocks:Email:Token is required");
    opts.EmailConnection.FromAddress = builder.Configuration["AuthBlocks:Email:From"]
        ?? throw new InvalidOperationException("AuthBlocks:Email:From is required");

    opts.AdminUserSettings = new AdminUserSettings
    {
        UserName = builder.Configuration["AuthBlocks:Admin:UserName"]
            ?? throw new InvalidOperationException("AuthBlocks:Admin:UserName is required"),
        Email = builder.Configuration["AuthBlocks:Admin:Email"]
            ?? throw new InvalidOperationException("AuthBlocks:Admin:Email is required"),
        Password = builder.Configuration["AuthBlocks:Admin:Password"]
            ?? throw new InvalidOperationException("AuthBlocks:Admin:Password is required"),
    };
});

// --- AuthBlocksWeb (consumer web) wiring ------------------------------------
// The Users/Registrations grids call the AuthBlocks API over HTTP. This single
// host both serves that API and renders the pages, so the web clients point at
// the host's own origin. Trailing slash matters: HttpClient.BaseAddress + the
// relative "api/users" path only resolve correctly with one.
var selfBaseUrl = builder.Configuration["AuthBlocks:Web:ApiBaseUrl"]
    ?? throw new InvalidOperationException("AuthBlocks:Web:ApiBaseUrl is required");

// ConfigureAuthServices internally calls AddBlazorBlocksWeb(), which registers
// EditModalSaveContextHolder (the DI holder the ModelView pages require).
AuthBlocksWeb.Startup.ConfigureAuthServices(builder.Services, selfBaseUrl);

builder.Services.AddControllers();

// Render-mode wiring mirrors the consumers: InteractiveServer shell + WASM
// assemblies registered for the AuthBlocksWeb.Client render-mode boundary.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Apply AuthBlocks EF migrations + seed system roles and the placeholder admin
// against local Postgres on first boot (authorized for this local DB).
await app.Services.UseAuthBlocksStartupAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

// Mount the AuthBlocks API surface (/api/users/*, /api/pending-registrations/*, etc.).
app.MapAuthBlocks();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AuthBlocksWeb._Imports).Assembly)
    .AddAdditionalAssemblies(typeof(AuthBlocksWeb.Client._Imports).Assembly)
    // Blazor page authorization is owned by AuthorizeRouteView in Routes.razor;
    // AuthBlocks JWTs live in browser localStorage and never reach the server on
    // a navigation, so without AllowAnonymous the JWT-bearer challenge would 401
    // an unauthenticated nav before the router can redirect to /account/login.
    .AllowAnonymous();

app.Run();
