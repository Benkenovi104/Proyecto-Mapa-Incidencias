using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

var builder = WebApplication.CreateBuilder(args);

// DbContext (elimina la configuración de NpgsqlDataSourceBuilder)
builder.Services.AddDbContext<AppDb>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
        npg => npg.UseNetTopologySuite())
);

// CORS y Swagger (igual)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
     .AllowAnyHeader().AllowAnyMethod()
));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

// Health
app.MapGet("/health", () => Results.Ok("ok"));

// Test DB
app.MapGet("/test-db", async (AppDb db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return ok ? Results.Ok(" Conectado a Supabase")
              : Results.Problem("No se pudo conectar");
});

/* ---------- ROLES ---------- */
app.MapGet("/roles", async (AppDb db) =>
    await db.Roles.OrderBy(r => r.Id).ToListAsync());

/* ---------- ESTADOS ---------- */
app.MapGet("/estado_incidencias", async (AppDb db) =>
    await db.EstadosIncidencia.OrderBy(e => e.Id).ToListAsync());

/* ---------- CATEGORÍAS ---------- */
app.MapGet("/categories", async (AppDb db) =>
    await db.Categorias
        .OrderBy(c => c.Id)
        .Select(c => new {
            c.Id,
            c.Nombre,
            c.IconoUrl
        })
        .ToListAsync());


/* ---------- INCIDENCIAS ---------- */
app.MapPost("/incidents", async ([FromBody] CreateIncidentDto dto, AppDb db) =>
{
    var gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    var point = gf.CreatePoint(new Coordinate(dto.Lon, dto.Lat));

    var inc = new Incidencia
    {
        User_Id = dto.UserId,
        Categoria_Id = dto.CategoriaId,
        Descripcion = dto.Descripcion,
        Ubicacion = point,
        Foto_Url = dto.FotoUrl,
        EstadoId = 1, // ← 1 = "nueva" (estado por defecto)
        Timestamp = DateTimeOffset.UtcNow
    };

    db.Incidencias.Add(inc);
    await db.SaveChangesAsync();

    return Results.Created($"/incidents/{inc.Id}", new { inc.Id });
});

// Obtener por bbox (ACTUALIZADO)
app.MapGet("/incidents", async (
    double minLon, double minLat, double maxLon, double maxLat, int? limit, AppDb db) =>
{
    var gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    var env = new Envelope(minLon, maxLon, minLat, maxLat);
    var box = gf.ToGeometry(env);

    var query = db.Incidencias
        .Include(i => i.Estado)
        .Include(i => i.Categoria) // ← INCLUIR CATEGORÍA
        .Where(i => i.Ubicacion != null && i.Ubicacion.Intersects(box))
        .OrderByDescending(i => i.Timestamp)
        .Select(i => new IncidentVm
        {
            Id = i.Id,
            CategoriaId = i.Categoria_Id,
            Descripcion = i.Descripcion!,
            FotoUrl = i.Foto_Url,
            Estado = i.Estado.Nombre,
            Lat = i.Ubicacion.Y,
            Lon = i.Ubicacion.X,
            Timestamp = i.Timestamp,
            IconoUrl = i.Categoria.IconoUrl // ← NUEVO: incluir el icono
        });

    return await query.Take(limit ?? 500).ToListAsync();
});

// Obtener por radio (ACTUALIZADO)
app.MapGet("/incidents/near", async (double lat, double lon, double radius, int? limit, AppDb db) =>
{
    var gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    var p = gf.CreatePoint(new Coordinate(lon, lat));

    var query = db.Incidencias
        .Include(i => i.Estado)
        .Include(i => i.Categoria) // ← INCLUIR CATEGORÍA
        .Where(i => i.Ubicacion.IsWithinDistance(p, radius))
        .OrderByDescending(i => i.Timestamp)
        .Select(i => new IncidentVm
        {
            Id = i.Id,
            CategoriaId = i.Categoria_Id,
            Descripcion = i.Descripcion!,
            FotoUrl = i.Foto_Url,
            Estado = i.Estado.Nombre,
            Lat = i.Ubicacion.Y,
            Lon = i.Ubicacion.X,
            Timestamp = i.Timestamp,
            IconoUrl = i.Categoria.IconoUrl // ← NUEVO: incluir el icono
        });

    return await query.Take(limit ?? 200).ToListAsync();
});

// Detalle de incidencia específica
app.MapGet("/incidents/{id:int}", async (int id, AppDb db) =>
{
    var incidente = await db.Incidencias
        .Include(i => i.Estado)
        .Include(i => i.Categoria)
        .Include(i => i.Usuario)
        .Where(i => i.Id == id)
        .Select(i => new IncidentDetailDto
        {
            Id = i.Id,
            UserId = i.User_Id,
            CategoriaId = i.Categoria_Id,
            CategoriaNombre = i.Categoria.Nombre,
            Descripcion = i.Descripcion!,
            FotoUrl = i.Foto_Url ?? "",
            Estado = i.Estado.Nombre,
            EstadoId = i.EstadoId,
            Lat = i.Ubicacion.Y,
            Lon = i.Ubicacion.X,
            Timestamp = i.Timestamp,
            UsuarioNombre = $"{i.Usuario.Nombre} {i.Usuario.Apellido}",
            UsuarioEmail = i.Usuario.Email
        })
        .FirstOrDefaultAsync();

    if (incidente is null)
        return Results.NotFound($"Incidencia con ID {id} no encontrada");

    return Results.Ok(incidente);
});

//  Filtrar incidencias por categoría y/o fecha
app.MapGet("/incidents/filter", async (
    [FromQuery] int? categoriaId,
    [FromQuery] DateTimeOffset? desde,
    [FromQuery] DateTimeOffset? hasta,
    [FromQuery] double? lat,
    [FromQuery] double? lon,
    [FromQuery] double? radius,
    [FromQuery] int? limit,
    AppDb db) =>
{
    // Construir query base
    var query = db.Incidencias
        .Include(i => i.Estado)
        .Include(i => i.Categoria)
        .AsQueryable();

    // Aplicar filtros
    if (categoriaId.HasValue)
    {
        query = query.Where(i => i.Categoria_Id == categoriaId.Value);
    }

    if (desde.HasValue)
    {
        query = query.Where(i => i.Timestamp >= desde.Value);
    }

    if (hasta.HasValue)
    {
        query = query.Where(i => i.Timestamp <= hasta.Value);
    }

    // Filtro por ubicación (radio) si se proporciona
    if (lat.HasValue && lon.HasValue && radius.HasValue)
    {
        var gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var center = gf.CreatePoint(new Coordinate(lon.Value, lat.Value));
        query = query.Where(i => i.Ubicacion.IsWithinDistance(center, radius.Value));
    }

    // Ejecutar query y proyectar
    var resultados = await query
        .OrderByDescending(i => i.Timestamp)
        .Take(limit ?? 100)
        .Select(i => new IncidentVm
        {
            Id = i.Id,
            CategoriaId = i.Categoria_Id,
            Descripcion = i.Descripcion!,
            FotoUrl = i.Foto_Url,
            Estado = i.Estado.Nombre,
            Lat = i.Ubicacion.Y,
            Lon = i.Ubicacion.X,
            Timestamp = i.Timestamp,
            IconoUrl = i.Categoria.IconoUrl // ← NUEVO: incluir el icono
        })
        .ToListAsync();

    return Results.Ok(resultados);
});

// Actualizar una incidencia existente
app.MapPut("/incidents/{id:int}", async (int id, [FromBody] UpdateIncidentDto dto, AppDb db) =>
{
    var incidente = await db.Incidencias.FindAsync(id);
    if (incidente is null)
        return Results.NotFound($"Incidencia con ID {id} no encontrada.");

    // Actualizar campos si vienen en el DTO
    if (dto.CategoriaId.HasValue)
        incidente.Categoria_Id = dto.CategoriaId.Value;

    if (!string.IsNullOrWhiteSpace(dto.Descripcion))
        incidente.Descripcion = dto.Descripcion;

    if (!string.IsNullOrWhiteSpace(dto.FotoUrl))
        incidente.Foto_Url = dto.FotoUrl;

    if (dto.EstadoId.HasValue)
        incidente.EstadoId = dto.EstadoId.Value;

    if (dto.Lat.HasValue && dto.Lon.HasValue)
    {
        var gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        incidente.Ubicacion = gf.CreatePoint(new NetTopologySuite.Geometries.Coordinate(dto.Lon.Value, dto.Lat.Value));
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Incidencia actualizada correctamente." });
});

// Eliminar una incidencia
app.MapDelete("/incidents/{id:int}", async (int id, AppDb db) =>
{
    var incidente = await db.Incidencias.FindAsync(id);
    if (incidente is null)
        return Results.NotFound($"Incidencia con ID {id} no encontrada.");

    db.Incidencias.Remove(incidente);
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = $"Incidencia {id} eliminada correctamente." });
});

// Exportar incidencias filtradas como CSV
app.MapGet("/incidents/export", async (
    [FromQuery] int? categoriaId,
    [FromQuery] DateTimeOffset? desde,
    [FromQuery] DateTimeOffset? hasta,
    AppDb db) =>
{
    var query = db.Incidencias
        .Include(i => i.Categoria)
        .Include(i => i.Estado)
        .AsQueryable();

    if (categoriaId.HasValue)
        query = query.Where(i => i.Categoria_Id == categoriaId.Value);

    if (desde.HasValue)
        query = query.Where(i => i.Timestamp >= desde.Value);

    if (hasta.HasValue)
        query = query.Where(i => i.Timestamp <= hasta.Value);

    var datos = await query
        .OrderByDescending(i => i.Timestamp)
        .Select(i => new
        {
            Id = i.Id,
            Categoria = i.Categoria.Nombre,
            Descripcion = i.Descripcion,
            Estado = i.Estado.Nombre,
            Latitud = i.Ubicacion.Y,
            Longitud = i.Ubicacion.X,
            Fecha = i.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        })
        .ToListAsync();

    using var writer = new StringWriter();
    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
    csv.WriteRecords(datos);

    var bytes = Encoding.UTF8.GetBytes(writer.ToString());
    return Results.File(bytes, "text/csv", "incidencias.csv");
});


/* ---------- USUARIOS ---------- */
app.MapGet("/users", async (AppDb db) =>
    await db.Usuarios
        .Include(u => u.Rol) // ← Incluir el rol
        .OrderBy(u => u.Id)
        .Select(u => new
        {
            u.Id,
            u.Nombre,
            u.Email,
            Rol = u.Rol.Nombre 
        })
        .ToListAsync()
);

/* ---------- CREAR USUARIO ---------- */
app.MapPost("/users", async (CreateUserDto dto, AppDb db) =>
{
    // Validar que el rol exista
    var rolExiste = await db.Roles.AnyAsync(r => r.Id == dto.RolId);
    if (!rolExiste)
    {
        return Results.BadRequest($"RolId inválido. Valores permitidos: 1 (vecino), 2 (administrador)");
    }

    var nuevo = new Usuario
    {
        Nombre = dto.Nombre,
        Apellido = dto.Apellido,
        Email = dto.Email,
        Username = dto.Username,
        Password = dto.Password, // ⚠️ pendiente: usar hashing
        RolId = dto.RolId,
        Fecha_Alta = DateTimeOffset.UtcNow
    };

    db.Usuarios.Add(nuevo);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{nuevo.Id}", new { nuevo.Id });
});

// Cambiar estado (actualizado)
app.MapPatch("/incidents/{id:int}/status", async (int id, [FromBody] int nuevoEstadoId, AppDb db) =>
{
    var inc = await db.Incidencias.FindAsync(id);
    if (inc is null) return Results.NotFound();

    // Validar que el estado exista
    var estadoExiste = await db.EstadosIncidencia.AnyAsync(e => e.Id == nuevoEstadoId);
    if (!estadoExiste)
    {
        return Results.BadRequest("EstadoId inválido");
    }

    inc.EstadoId = nuevoEstadoId;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Cambiar rol de un usuario
app.MapPatch("/users/{id:int}/role", async (int id, [FromBody] ChangeRoleDto dto, AppDb db) =>
{
    var user = await db.Usuarios.FindAsync(id);
    if (user is null)
        return Results.NotFound($"Usuario con ID {id} no encontrado.");

    var nuevoRolNombre = dto.Rol.Trim().ToLower();

    // Busca el rol en la tabla roles
    var rol = await db.Roles.FirstOrDefaultAsync(r => r.Nombre.ToLower() == nuevoRolNombre);
    if (rol is null)
        return Results.BadRequest($"Rol '{nuevoRolNombre}' no encontrado. Crea el rol primero o usa uno válido.");

    user.RolId = rol.Id;
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = $"Rol del usuario {user.Nombre} actualizado a '{rol.Nombre}'." });
});


// Registro básico de usuario
app.MapPost("/auth/register", async ([FromBody] RegisterDto dto, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Email y contraseña son obligatorios.");

    var existe = await db.Usuarios.AnyAsync(u => u.Email == dto.Email);
    if (existe)
        return Results.BadRequest("Ya existe un usuario registrado con ese email.");

    // Busca el rol 'vecino' en la tabla roles
    var rolVecino = await db.Roles.FirstOrDefaultAsync(r => r.Nombre.ToLower() == "vecino");
    if (rolVecino is null)
        return Results.BadRequest("El rol 'vecino' no existe en la base de datos.");

    var nuevo = new Usuario
    {
        Nombre = dto.Nombre,
        Apellido = dto.Apellido,
        Email = dto.Email,
        Username = dto.Username,
        Password = dto.Password, // 🔒 luego lo hashamos
        RolId = rolVecino.Id,
        Fecha_Alta = DateTimeOffset.UtcNow
    };

    db.Usuarios.Add(nuevo);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{nuevo.Id}", new
    {
        nuevo.Id,
        nuevo.Nombre,
        nuevo.Apellido,
        nuevo.Email,
        nuevo.Username,
        Rol = rolVecino.Nombre
    });
});

// Login básico
app.MapPost("/auth/login", async ([FromBody] LoginDto dto, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Email y contraseña son obligatorios.");

    var user = await db.Usuarios
        .Include(u => u.Rol)
        .FirstOrDefaultAsync(u => u.Email == dto.Email);

    if (user is null)
        return Results.NotFound("Usuario no encontrado.");

    // 🔒 Comparación simple (más adelante se hace con hash)
    if (user.Password != dto.Password)
        return Results.Unauthorized();

    // Simulamos creación de "sesión" (por ahora sin JWT)
    var session = new
    {
        user.Id,
        user.Nombre,
        user.Apellido,
        user.Email,
        user.Username,
        Rol = user.Rol.Nombre,
        LoginTime = DateTimeOffset.UtcNow
    };

    return Results.Ok(session);
});


// Logout (dummy)
app.MapPost("/auth/logout", () =>
{
    // En una app real, acá se invalidaría el token o sesión activa
    return Results.Ok(new { message = "Logout exitoso" });
});

// Cambiar contraseña
app.MapPost("/auth/change-password", async ([FromBody] ChangePasswordDto dto, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) ||
        string.IsNullOrWhiteSpace(dto.OldPassword) ||
        string.IsNullOrWhiteSpace(dto.NewPassword))
        return Results.BadRequest("Faltan datos obligatorios.");

    var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);

    if (user is null)
        return Results.NotFound("Usuario no encontrado.");

    if (user.Password != dto.OldPassword)
        return Results.BadRequest("La contraseña actual es incorrecta.");

    user.Password = dto.NewPassword; // 🔒 En producción, deberías hashearla
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Contraseña actualizada correctamente." });
});


// Endpoint opcional para actualizar automáticamente las URLs de iconos
app.MapPost("/categories/update-icons", async (AppDb db) =>
{
    // ⚠️ CAMBIAR ESTE BASE URL CON TU PROJECT ID REAL DE SUPABASE
    string baseUrl = "https://gsjjoqajnpfxvjusettt.supabase.co/storage/v1/object/public/LogosCategorias/";

    var categorias = await db.Categorias.ToListAsync();
    foreach (var cat in categorias)
    {
        cat.IconoUrl = $"{baseUrl}{cat.Nombre.ToLower()}.png";
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "URLs de iconos actualizadas correctamente." });
});



// ======================
// 👤 PERFIL DE USUARIO
// ======================

// Obtener datos del usuario por ID
app.MapGet("/users/{id:int}", async (int id, AppDb db) =>
{
    var user = await db.Usuarios
        .Include(u => u.Rol)
        .Where(u => u.Id == id)
        .Select(u => new
        {
            u.Id,
            u.Nombre,
            u.Apellido,
            u.Username,
            u.Email,
            Rol = u.Rol.Nombre
        })
        .FirstOrDefaultAsync();

    if (user is null)
        return Results.NotFound($"Usuario con ID {id} no encontrado");

    return Results.Ok(user);
});

// Actualizar perfil de usuario
app.MapPut("/users/{id:int}", async (int id, [FromBody] UpdateUserDto dto, AppDb db) =>
{
    var user = await db.Usuarios.FindAsync(id);
    if (user is null)
        return Results.NotFound($"Usuario con ID {id} no encontrado");

    // Verificar si el email ya existe en otro usuario
    if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
    {
        var emailExists = await db.Usuarios.AnyAsync(u => u.Email == dto.Email && u.Id != id);
        if (emailExists)
            return Results.BadRequest("Ya existe un usuario con ese email");
    }

    // Verificar si el username ya existe en otro usuario
    if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
    {
        var usernameExists = await db.Usuarios.AnyAsync(u => u.Username == dto.Username && u.Id != id);
        if (usernameExists)
            return Results.BadRequest("Ya existe un usuario con ese nombre de usuario");
    }

    // Actualizar campos
    if (!string.IsNullOrWhiteSpace(dto.Nombre))
        user.Nombre = dto.Nombre;

    if (!string.IsNullOrWhiteSpace(dto.Apellido))
        user.Apellido = dto.Apellido;

    if (!string.IsNullOrWhiteSpace(dto.Username))
        user.Username = dto.Username;

    if (!string.IsNullOrWhiteSpace(dto.Email))
        user.Email = dto.Email;

    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Perfil actualizado correctamente" });
});

// Eliminar cuenta de usuario
app.MapDelete("/users/{id:int}", async (int id, AppDb db) =>
{
    var user = await db.Usuarios.FindAsync(id);
    if (user is null)
        return Results.NotFound($"Usuario con ID {id} no encontrado");

    // Opcional: verificar si el usuario tiene incidencias y decidir qué hacer
    var tieneIncidencias = await db.Incidencias.AnyAsync(i => i.User_Id == id);
    if (tieneIncidencias)
    {
        // Opción 1: Eliminar también las incidencias
        var incidencias = db.Incidencias.Where(i => i.User_Id == id);
        db.Incidencias.RemoveRange(incidencias);

        // Opción 2: O mantener las incidencias pero asignarlas a un usuario genérico
        // var incidencias = db.Incidencias.Where(i => i.User_Id == id);
        // foreach (var inc in incidencias)
        // {
        //     inc.User_Id = 1; // ID de usuario genérico
        // }
    }

    db.Usuarios.Remove(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Cuenta eliminada correctamente" });
});

// Endpoint específico para crear operadores (administradores)
app.MapPost("/auth/register-admin", async ([FromBody] RegisterDto dto, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest("Email y contraseña son obligatorios.");

    var existe = await db.Usuarios.AnyAsync(u => u.Email == dto.Email);
    if (existe)
        return Results.BadRequest("Ya existe un usuario registrado con ese email.");

    // Buscar el rol "administrador"
    var rolAdmin = await db.Roles.FirstOrDefaultAsync(r => r.Nombre.ToLower() == "administrador");
    if (rolAdmin is null)
        return Results.BadRequest("El rol 'administrador' no existe en la base de datos.");

    var nuevo = new Usuario
    {
        Nombre = dto.Nombre,
        Apellido = dto.Apellido,
        Email = dto.Email,
        Username = dto.Username,
        Password = dto.Password,
        RolId = rolAdmin.Id, // Siempre rol administrador
        Fecha_Alta = DateTimeOffset.UtcNow
    };

    db.Usuarios.Add(nuevo);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{nuevo.Id}", new
    {
        nuevo.Id,
        nuevo.Nombre,
        nuevo.Apellido,
        nuevo.Email,
        nuevo.Username,
        Rol = rolAdmin.Nombre
    });
});

app.Run();