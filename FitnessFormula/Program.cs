using FitnessFormula.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Npgsql; // ������, ���� ���



var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.UseUrls("http://192.168.95.86:7112", "https://192.168.95.86:7113"); // �������� � �������� IP

//builder.WebHost.UseUrls("http://192.168.242.86:7112", "https://192.168.242.86:7113"); // �������� � �������� IP

// ��������� CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin() // ��������� ������� � ����� �������
               .AllowAnyMethod()  // ��������� ����� HTTP ������
               .AllowAnyHeader(); // ��������� ����� ���������
    });
});

//services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));


// ����������� � ��
builder.Services.AddDbContext<FitnessDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ���������� ������������ � Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseStaticFiles();


// ���������� CORS ��������
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
