
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using System.Runtime.InteropServices;
//using JWTAuthentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Utils.JWTAuthentication;
using Utils.FileHelper;
using System.Diagnostics;
using APIFileServer.source;
using Serilog;
using System.Reflection;

namespace APIFileServer
{
    public class Program
    {
        public static JWTSecureConfiguration? Secure { get; private set; } = null;
        public static Serilog.ILogger Logger { get; private set; }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var sharedFileConf = builder.Configuration.GetValue<string>("LoggerPath");

            string fileName = Path.Combine(sharedFileConf, $"{DateTime.Now}_Patcher.LOG");

            Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(fileName, Serilog.Events.LogEventLevel.Debug, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Logger.Information($"Starting PATCHER software {Assembly.GetExecutingAssembly().GetName().Version}");

            Stopwatch st = new Stopwatch();
            st.Start();
            RestAPIConfiguration restConf = new RestAPIConfiguration(builder.Configuration, Logger).CreateFileList().MakeChunksFiles();//.FillCache(RestCache);
            st.Stop();
            Logger.Information($"Size list {restConf.FileList.TotalFileSize} bytes, {st.ElapsedMilliseconds}ms");

            RestAPIFileCache RestCache = new RestAPIFileCache(restConf.MaxCacheRam);

            Secure = restConf.JWTConfig ?? new JWTSecureConfiguration();

            if (!Directory.Exists(restConf.SharedFilePath))
            {
                throw new FileNotFoundException(restConf.SharedFilePath);
            }

            var physicalProvider = new PhysicalFileProvider(restConf.PhysicalFileRoot);

            builder.Services.AddSingleton<IFileProvider>(physicalProvider);

            if(restConf.FileList is not null)
                builder.Services.AddSingleton(restConf.FileList);

            builder.Services.AddSingleton(RestCache);
            builder.Services.AddSingleton(Secure);
            //builder.Services.AddScoped<JwtUtils, JwtUtils>();
            builder.Services.AddDirectoryBrowser();

            if (restConf.JWTIsEnabled)
            {
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
            }


            // Add services to the container.
            builder.WebHost.UseKestrel(x => { x.Limits.MaxConcurrentConnections = 1000; });
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (restConf.OpenFileProvider)
            {
                app.UseDirectoryBrowser(new DirectoryBrowserOptions
                {
                    FileProvider = physicalProvider,
                    RequestPath = restConf.UrlFileProvider
                });
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else //utilizzo le configurazioni del profilo 
            {
                Uri hostUri = new UriBuilder("http", restConf.Host, restConf.HTTPPort).Uri;
                Uri hostUriHttps = new UriBuilder("https", restConf.Host, restConf.HTTPSPort).Uri;


                app.Urls.Add(hostUri.AbsoluteUri);
                app.Urls.Add(hostUriHttps.AbsoluteUri);
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            //app.MapControllers();
            //app.MapControllerRoute(
            //    name: "FileApi",
            //    pattern: "api/FileApi/{action=Index}/{id?}",
            //    defaults: new { controller = "File", action = "Index" });

            //app.MapControllerRoute(
            //    name: "default", //Route Name
            //    pattern: "{controller=Home}/{action=Index}/{id}" //Route Pattern
            //);
            //app.MapControllerRoute(
            //    name: "File",
            //    pattern: "File/{}",
            //    defaults: new { controller = "File", action = "Index" });

            app.MapControllers();
            app.Run();
        }
    }
}
