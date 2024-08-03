using ConfigurationWebPage.Application.Pipelines.Caching;
using ConfigurationWebPage.Application.Pipelines.MessageBroker;
using ConfigurationWebPage.Models;
using ConfigurationWebPage.Services;
using MassTransit;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));

// ConfigurationService'i hizmet olarak ekleyin
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    configuration.AddOpenBehavior(typeof(CachingBehavior<,>));
    configuration.AddOpenBehavior(typeof(CacheRemovingBehavior<,>));
    configuration.AddOpenBehavior(typeof(RabbitMQServiceBehavior<,>));

  
});

builder.Services.AddStackExchangeRedisCache(opt=>opt.Configuration="localhost:6379");
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/ConfigurationSetting/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ConfigurationSetting}/{action=Index}/{id?}");

app.Run();
