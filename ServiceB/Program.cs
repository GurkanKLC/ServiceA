using ConfigurationReaderLibrary;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new ConfigurationReader(
    applicationName: "SERVICE-B",
    connectionString: "mongodb://root:example@localhost:27017/",
    refreshTimerIntervalInMs: 60000 // 60 saniye
));
// Add services to the container.
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
