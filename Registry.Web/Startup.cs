using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Registry.Web.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Registry.Web
{
    public class Startup
    {
        private const string IdentityConnectionName = "IdentityConnection";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();

            // Let's use a strongly typed class for settings
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<AppSettings>();

            ConfigureAuthProvider(services, appSettings.AuthProvider);

            services.AddIdentityCore<User>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager();


            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(auth =>
                {
                    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(jwt =>
                {
                    jwt.RequireHttpsMetadata = false;
                    jwt.SaveToken = true;
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 1;
                options.Password.RequiredUniqueChars = 0;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = false;
            });

            // Error messages that make sense
            services.Configure<ApiBehaviorOptions>(o =>
            {
                o.InvalidModelStateResponseFactory = actionContext =>
                    new BadRequestObjectResult(new ErrorResponse(actionContext.ModelState));
            });

        }

        private void ConfigureAuthProvider(IServiceCollection services, AuthProvider authProvider)
        {
            switch (authProvider)
            {
                case AuthProvider.Sqlite:



                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlite(
                            Configuration.GetConnectionString(IdentityConnectionName)));

                    break;
                case AuthProvider.Mysql:

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseMySql(
                            Configuration.GetConnectionString(IdentityConnectionName)));

                    break;
                case AuthProvider.Mssql:

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(
                            Configuration.GetConnectionString(IdentityConnectionName)));

                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unrecognised auth provider: '{authProvider}'");
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // We are permissive now
            app.UseCors(cors => cors
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

            });

            UpdateDatabase(app);

        }

        private void UpdateDatabase(IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

            // NOTE: We support migrations only for sqlite
            if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {

                EnsureFolderCreated(Configuration.GetConnectionString(IdentityConnectionName));

                context.Database.Migrate();
            }
        }

        /// <summary>
        /// Ensures that the sqlite database folder exists 
        /// </summary>
        /// <param name="connstr"></param>
        private void EnsureFolderCreated(string connstr)
        {
            var segments = connstr.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                var fields = segment.Split('=');

                if (string.Equals(fields[0], "Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    var dbPath = fields[1];

                    var folder = Path.GetDirectoryName(dbPath);

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }
            }
        }
    }
}