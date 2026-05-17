# Cerebellum.AuthBlocks

A convenience library built on top of ASP.NET Core Identity for .NET 10, designed for rapid authentication and authorization integration within the [BlazorBlocks](https://github.com/daniel-c-harvey/BlazorBlocks) application ecosystem.

## Overview

AuthBlocks reduces authentication boilerplate to a single `AddAuthBlocks(...)` call. It wires together JWT Bearer authentication, ASP.NET Core Identity, hierarchical role authorization, email-based pending-registration flows, and EF Core startup seeding — purpose-built for Cerebellum Softworks' BlazorBlocks-based full-stack applications but usable in any ASP.NET Core Web API host.

## Features

- **JWT Bearer authentication** — full token validation configured via a single options object; no manual `AddAuthentication` boilerplate
- **ASP.NET Core Identity integration** — ships `ApplicationUser`, `ApplicationRole`, and all related entity types ready for EF Core
- **Hierarchical role authorization** — roles inherit permissions from parent roles; a cached `IHierarchicalRoleService` handles transitive role checks transparently, replacing the default ASP.NET Core role handler
- **Pending registration flow** — token-based invitation system with email delivery via Mailtrap
- **Startup seeding** — applies EF migrations on boot, seeds system roles, and optionally seeds an admin user on first run
- **Pre-built minimal API routes** — ready-to-map endpoints for `/auth`, `/users`, `/roles`, `/user-roles`, and `/pending-registrations`

## Installation

```shell
dotnet add package Cerebellum.AuthBlocks
```

> AuthBlocks targets **net10.0** and requires a compatible ASP.NET Core Web API host.

## Quick Start

### 1. Register services

```csharp
builder.Services.AddAuthBlocks(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    options.ApplicationName  = "MyApp";
    options.SupportEmail     = "support@example.com"; // optional — shown in registration emails

    options.JwtSettings.Secret         = builder.Configuration["Jwt:Secret"]!;
    options.JwtSettings.Issuer         = builder.Configuration["Jwt:Issuer"]!;
    options.JwtSettings.Audience       = builder.Configuration["Jwt:Audience"]!;

    options.EmailConnection.Host  = builder.Configuration["Email:Host"]!;
    options.EmailConnection.Token = builder.Configuration["Email:Token"]!;

    // Optional: seed an admin user on first boot
    options.AdminUserSettings = new AdminUserSettings
    {
        UserName = "admin",
        Email    = "admin@example.com",
        Password = builder.Configuration["Admin:Password"]!
    };
});
```

### 2. Run startup tasks

```csharp
var app = builder.Build();

// Applies pending EF migrations, seeds system roles, and optionally seeds the admin user.
await app.Services.UseAuthBlocksStartupAsync();
```

### 3. Map auth routes

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthBlocks(); // registers /auth, /users, /roles, /user-roles, /pending-registrations
```

## Hierarchical Roles

AuthBlocks replaces the default ASP.NET Core role authorization handler with one that understands role inheritance. Roles are arranged in a parent–child tree. A user assigned a higher-privilege parent role (e.g. `Admin`) automatically satisfies authorization checks that require any of its descendant roles (e.g. `Staff`, `Viewer`). Privilege flows downward — granting a parent role implicitly grants access to everything the child roles protect.

Role hierarchy is seeded automatically from `SystemRole` definitions on startup — no manual database setup required.

## Dependencies

| Package | Purpose |
|---|---|
| `Cerebellum.NetBlocks` | Result types, environment config helpers |
| `Cerebellum.NetBlocks.Models` | Shared model primitives |
| `Cerebellum.BlazorBlocks.Api` | Shared API contracts for the BlazorBlocks ecosystem |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT Bearer middleware |

## License

[AGPL-3.0-or-later](https://www.gnu.org/licenses/agpl-3.0.html)
