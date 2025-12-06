using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure and register the MongoDB service
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoDbService>();

// *** ADD AUTHENTICATION SERVICES HERE ***
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Where to redirect unauthenticated users
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });


var app = builder.Build();

// Seed the database with a default admin user if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var mongoDbService = services.GetRequiredService<MongoDbService>();
    var existingUser = await mongoDbService.Users.Find(u => u.Username == "admin").FirstOrDefaultAsync();
    if (existingUser == null)
    {
        var adminUser = new User
        {
            Username = "admin",
            // *** CHANGED: Use FirstName and LastName ***
            FirstName = "Default",
            LastName = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        await mongoDbService.Users.InsertOneAsync(adminUser);
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// *** ADD AUTHENTICATION MIDDLEWARE HERE (MUST BE BEFORE AUTHORIZATION) ***
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

