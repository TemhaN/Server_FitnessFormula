using FitnessFormula.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql; // добавь, если нет



var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.UseUrls("http://192.168.95.86:7112", "https://192.168.95.86:7113"); // Привязка к рабочему IP

//builder.WebHost.UseUrls("http://192.168.242.86:7112", "https://192.168.242.86:7113"); // Привязка к рабочему IP

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin() // Разрешить запросы с любых доменов
               .AllowAnyMethod()  // Разрешить любые HTTP методы
               .AllowAnyHeader(); // Разрешить любые заголовки
    });
});

//services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));


// Подключение к БД
builder.Services.AddDbContext<FitnessDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавление контроллеров и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseStaticFiles();


// Применение CORS политики
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
