using BankingApp.Application.Features.Account.Command.Create;
using BankingApp.Domain.Utility;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.Context;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using BankingApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace BankingApp.API.Extensions
{
    public static class ServiceRegistration
    {
        
        public static void AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Fast Api",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    //var key = Encoding.UTF8.GetBytes(configuration.GetSection("JWT:Key").Value);
                    var jwtKey = configuration.GetSection("JWT:Key").Value;

                    if (string.IsNullOrEmpty(jwtKey))
                    {
                        throw new ArgumentNullException(nameof(jwtKey), "JWT key is missing in the configuration.");
                    }

                    var key = Encoding.UTF8.GetBytes(jwtKey);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = false,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateLifetime = false,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                    };
                });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Register Application Services
            var assembly = typeof(CreateAccountHandler).Assembly; 
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assembly));
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IPaymentGateway, MockPaystackService>();
            services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            services.AddScoped<IUnitOfWork, UnitOfWork<BankingDbContext>>();



            //other services
            services.AddHttpContextAccessor();

        }
    }

}
