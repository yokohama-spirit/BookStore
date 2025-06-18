using AuthServiceLibrary.Application.Requests;
using AuthServiceLibrary.Application.Services;
using AuthServiceLibrary.Domain.Interfaces;
using AuthServiceLibrary.Infrastructure.Data;
using BookServiceLibrary.Application.Requests;
using BookServiceLibrary.Application.Services;
using BookServiceLibrary.Domain.Interfaces;
using BookServiceLibrary.Infrastructure.Data;
using BookServiceLibrary.Infrastructure.Repositories;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
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
builder.Services.AddAuthorization();


// Dependency injection
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<MongoDBService>();
builder.Services.AddScoped<IUserSupport, UserSupport>();
builder.Services.AddScoped<IBookSearchService, BookSearchService>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();


//Mediator
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateBookRequest).Assembly);
});


// Mapp settings
builder.Services.AddAutoMapper(typeof(BookProfile));


//Elasticsearch Config
builder.Services.Configure<ElasticsearchSettings>(builder.Configuration.GetSection("Elasticsearch"));

builder.Services.AddScoped(sp =>
{
    var uri = sp.GetRequiredService<IOptions<ElasticsearchSettings>>().Value.Uri;
    return new ElasticsearchClient(new Uri(uri));
});


// RabbitMQ congif
var rabbitMqConnection = new ConnectionFactory
{
    HostName = "localhost"
}.CreateConnection();

builder.Services.AddSingleton(rabbitMqConnection);





// Конфигурация Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

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
