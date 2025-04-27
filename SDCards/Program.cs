using SDCards.Controllers;
using SDCards.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddDistributedMemoryCache(); 
builder.Services.AddSession();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();  // ✅ You need this line to serve wwwroot assets

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapHub<SwipeHub>("/swipeHub");

// Map controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=DecisionRoom}/{action=CreateRoom}/{id?}");

app.Run();