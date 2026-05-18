using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using JanaApi.Dapper;
using JanaApi.Service;
using JanaApi.Utilities;
using System.Text;

namespace JanaApi
{
    public class Program
    {
        public static string _connectionString = null;
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            _connectionString = configuration.GetConnectionString("DefaultConnection");

            var jwtSection = configuration.GetSection("JWT");
            var JWTKey = jwtSection.GetValue<string>("JWTKey");
            var JWTIssuer = jwtSection.GetValue<string>("JWTIssuer");
            var JTWAudience = jwtSection.GetValue<string>("JTWAudience");
            double JWTExpireTime = jwtSection.GetValue<double>("JWTExpireTime");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidIssuer = JWTIssuer,
                       ValidateAudience = true,
                       ValidAudience = JTWAudience,
                       ValidateIssuerSigningKey = true,
                       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTKey))
                   };
               });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireClaim("admin", "true"));
            });

            builder.Services.AddSwaggerGen(c =>
            {
                //c.SwaggerDoc("v2", new OpenApiInfo { Title = "DataSample API", Version = "v2" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
              {
                {
                  new OpenApiSecurityScheme
                  {
                    Reference = new OpenApiReference
                      {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                      },
                      Scheme = "oauth2",
                      Name = "Bearer",
                      In = ParameterLocation.Header,

                    },
                    new List<string>()
                  }
                });

                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

            builder.Services.AddCors(config =>
            {
                config.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
                });
            });

            //DI
            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.AddScoped<IDapperContext, DapperContext>((_) => new DapperContext(_connectionString));
            builder.Services.AddScoped<IJwtHelper, JwtHelper>((_) => new JwtHelper(JWTKey, JWTIssuer, JTWAudience, JWTExpireTime));
            builder.Services.AddScoped<IFolderService, FolderService>();
            builder.Services.AddScoped<INoteService, NoteService>();
            builder.Services.AddScoped<IUsersService, UsersService>();

            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false,
                KnownProxies = { },
                KnownNetworks = { }
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            //db folder
            CheckDbFolder();
            //migration
            DbMigrator.MigrateUp(_connectionString);

            // Update admin password from environment if provided
            await UpdateAdminPasswordFromEnvironment(app.Services);

            if (app.Environment.IsDevelopment())
            {
                app.Run();
            }
            else
            {
                app.Run("http://0.0.0.0:8080");
            }
        }

        private static async Task UpdateAdminPasswordFromEnvironment(IServiceProvider services)
        {
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            if (!string.IsNullOrEmpty(adminPassword))
            {
                using (var scope = services.CreateScope())
                {
                    var usersService = scope.ServiceProvider.GetRequiredService<IUsersService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    
                    try
                    {
                        await usersService.UpdateAdminPasswordAsync(adminPassword);
                        logger.LogInformation("Admin password updated from environment variable");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to update admin password from environment variable");
                    }
                }
            }
        }

        private static void CheckDbFolder()
        {
            if (!Directory.Exists("db"))
            {
                Directory.CreateDirectory("db");
            }
        }
    }
}
