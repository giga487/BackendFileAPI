
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using System.Runtime.InteropServices;
using JWTAuthentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace APIFileServer
{
    public class RestAPIConfiguration
    {
        public int HTTPPort { get; private set; } = 5000;
        public int HTTPSPort { get; private set; } = 5001;
        public int RestTImeOutMS { get; private set; } = 10000;
        public string SharedFilePath { get; private set; } = string.Empty;
        public string ServerWebPath { get; private set; } = string.Empty;
        public bool OpenFileProvider { get; private set; } = false;
        public string Host { get; private set; } = string.Empty;
        public RestAPIConfiguration(ConfigurationManager config)
        {
            var sharedFileConf = config.GetSection("SharedFile");
            SharedFilePath = sharedFileConf.GetValue<string>("ClientFiles") ?? string.Empty;

            if (SharedFilePath == string.Empty)
            {
                throw new ArgumentException("No file path to share");
            }

            OpenFileProvider = sharedFileConf.GetValue<bool>("OpenFileProvider");
            Host = sharedFileConf.GetValue<string>("Host") ?? string.Empty;

            if (Host == string.Empty)
            {
                Host = "Localhost";
            }

            ServerWebPath = sharedFileConf.GetValue<string>("ServerFilesPath") ?? string.Empty;
        }
    }

    public class Program
    {
        public static SecureConfigurator Secure { get; private set; } = null;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            RestAPIConfiguration restConf = new RestAPIConfiguration(builder.Configuration);
            Secure = new SecureConfigurator();

            var physicalProvider = new PhysicalFileProvider(restConf.SharedFilePath);
            builder.Services.AddSingleton<IFileProvider>(physicalProvider);

            builder.Services.AddSingleton(Secure);
            builder.Services.AddScoped<JwtUtils, JwtUtils>();
            builder.Services.AddDirectoryBrowser();

            Uri hostUri = new UriBuilder("http", restConf.Host, restConf.HTTPPort).Uri;
            Uri hostUriHttps = new UriBuilder("https", restConf.Host, restConf.HTTPSPort).Uri;

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Secure.Audience,
                    ValidIssuer = Secure.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secure.MyKey))
                };
            });

            var app = builder.Build();

            app.Urls.Add(hostUri.AbsoluteUri);
            app.Urls.Add(hostUriHttps.AbsoluteUri);
            //app.Urls.Add(hostUri.AbsoluteUri);
            //app.Urls.Add(hostUriHttps.AbsoluteUri);

            if (restConf.OpenFileProvider)
            {
                app.UseDirectoryBrowser(new DirectoryBrowserOptions
                {
                    FileProvider = physicalProvider,
                    RequestPath = restConf.ServerWebPath
                });
            }

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
        }
    }
}
