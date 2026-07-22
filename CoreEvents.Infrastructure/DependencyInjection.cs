using System.Text;
using CoreEvents.Application.Interfaces.Identity;
using CoreEvents.Application.Interfaces.Locks;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Infrastructure.Data;
using CoreEvents.Infrastructure.Identity;
using CoreEvents.Infrastructure.Locks;
using CoreEvents.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddTransient<ITokenProvider, JwtTokenProvider>();
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAcc) =>
                {
                    var jwtOptions = jwtOptionsAcc.Value;

                    bearerOptions.MapInboundClaims = false;

                    bearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        RoleClaimType = "role",

                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero,
                        
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                    };
                });
            services.AddAuthorization();

            services.AddSingleton<ITokenProvider, JwtTokenProvider>();
            services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();

            services.AddDataBase(configuration, environment);
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
        public static IServiceCollection AddDataBase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                if (!environment.IsProduction())
                {
                    options
                        .LogTo(Console.WriteLine)
                        .EnableDetailedErrors();
                }
            });

            services.AddScoped<ILockProvider, PostgresTransactionLockProvider>();
            return services;
        }
    }
}
