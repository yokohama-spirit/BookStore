using AuthServiceLibrary.Application.Requests;
using AuthServiceLibrary.Application.Services;
using AuthServiceLibrary.Domain.Interfaces;
using AuthServiceLibrary.Domain.Interfaces.Admin;
using AuthServiceLibrary.Infrastructure.Data;
using AuthServiceLibrary.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Jwt
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };

    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrRoot", policy =>
        policy.RequireRole("Admin", "Root"));
});


// Dependency injection
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddScoped<UserSupportForUsers>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<RabbitMQConsumerService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();




//Mediator
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateUserRequest).Assembly);
});


// Mapp settings
builder.Services.AddAutoMapper(typeof(UserProfile));


// RabbitMQ congif
var factory = new ConnectionFactory { HostName = "localhost" };
var connection = factory.CreateConnection();

builder.Services.AddSingleton(connection);

var channel = connection.CreateModel();


channel.QueueDeclare(
    queue: "edit_user_balance",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
