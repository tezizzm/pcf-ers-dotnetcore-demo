using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Articulate;
using Articulate.Models;
using Articulate.Repositories;
using Articulate.Workaround;
using Articulate.Workaround.AzureSpringApps;
using Articulate.Workaround.Config.Properties;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.SpringCloud.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;
using Steeltoe.Common.Http;
using Steeltoe.Common.Http.Discovery;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
// using Steeltoe.Bootstrap.Autoconfig;
using Steeltoe.Connector.EFCore;
using Steeltoe.Connector.MySql;
using Steeltoe.Connector.Services;
using Steeltoe.Connector.MySql.EFCore;
using Steeltoe.Connector.SqlServer;
using Steeltoe.Connector.SqlServer.EFCore;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Client.SimpleClients;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.Placeholder;
using Steeltoe.Extensions.Configuration.RandomValue;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.TaskCore;
using Steeltoe.Management.Tracing;
using Steeltoe.Security.Authentication.CloudFoundry;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Extensions.Configuration.ConfigServer;
using LocalCertificateWriter = Articulate.LocalCerts.LocalCertificateWriter;

var logger = LoggerFactory.Create(c => c
    .AddSimpleConsole(f => f
        .SingleLine = true))
    .CreateLogger<Program>();
var builder = WebApplication.CreateBuilder(args);
builder.UseCloudFoundryCertificateForInternalRoutes();
builder.WebHost.UseAzureSpringCloudService();
// when running locally, get config from <gitroot>/config folder
// string configDir = "../config/";
// var appName = typeof(Program).Assembly.GetName().Name;
builder.Configuration
    .AddYamlFile("appsettings.yaml", false, true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", true, true)
    .AddProfiles()
    .AddEnvironmentVariables()
    .AddCommandLine(args);


if (Platform.IsCloudFoundry)
{
    builder.Configuration
        .AddCloudFoundry()
        .AddCloudFoundryContainerIdentity();
    if (builder.Environment.IsDevelopment())
    {
        builder.UseDevCertificate();
    }
}

if (Platform2.IsAzureSpringApps)
{
    builder.Configuration.AddAzureConfiguration();

    var certPath = Regex.Replace(Environment.GetEnvironmentVariable("eureka_client_tls_keystore"), "^file://", "");
    builder.Configuration.AddCertificateFile(certPath);
    var certificateSource = (builder.Configuration as IConfigurationBuilder).Sources.FirstOrDefault(cSource => cSource is ICertificateSource);
    if (certificateSource != null)
    {
        var certConfigurer =
            Activator.CreateInstance((certificateSource as ICertificateSource).OptionsConfigurer, builder.Configuration)
                as IConfigureNamedOptions<CertificateOptions>;
        var certOptions = new CertificateOptions();
        certConfigurer.Configure(certOptions);
    }
}


var features = new EnvironmentInfo(builder.Configuration);

builder.Services.AddSingleton(features);

//todo: this code should belong in steeltoe


if (features.IsConfigServerBound)
{
    builder.Configuration
        .AddConfigServer()
        // readadd profiles and env vars on top of values from config server as they may alter selection of which keys to use
        .AddProfiles() 
        .AddEnvironmentVariables();
}
else
{
    logger.LogWarning("Config server not being used as no binding information was present");
}


builder.Configuration
    .AddCommandLine(args)
    .AddInMemoryCollection(new Dictionary<string, string> // used to demonstrate how random and placeholder provider is used
    {
        { "MyConnectionString", "Server=${SqlHost};Database=${SqlDatabase};User Id=${SqlUser};Password=${SqlPassword};" },
        { "AppInstanceId", "${random:value}" }
    })
    .AddRandomValueSource()
    .AddPlaceholderResolver();


builder.AddAllActuators();
var services = builder.Services;

if (Platform.IsCloudFoundry)
{
    
    
    services.AddCloudFoundryContainerIdentity();
    services.AddAuthentication((options) =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CloudFoundryDefaults.AuthenticationScheme;
        })
        .AddCookie((options) =>
        {
            options.AccessDeniedPath = new PathString("/Home/AccessDenied");
        })
        .AddCloudFoundryOAuth(builder.Configuration)
        .AddCloudFoundryIdentityCertificate();

    services.AddAuthorization(cfg =>
    {
        cfg.AddPolicy(SecurityPolicy.LoggedIn, policy => policy
            .AddAuthenticationSchemes(CloudFoundryDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());
        cfg.AddPolicy(CloudFoundryDefaults.SameOrganizationAuthorizationPolicy, policy =>
        {
            policy.AuthenticationSchemes.Add(CertificateAuthenticationDefaults.AuthenticationScheme);
            policy.SameOrg();
        });
        cfg.AddPolicy(CloudFoundryDefaults.SameSpaceAuthorizationPolicy, policy =>
        {
            policy.AuthenticationSchemes.Add(CertificateAuthenticationDefaults.AuthenticationScheme);
            policy.SameSpace();
        });
    });
}

services.AddDistributedTracing();

//services.AddSpringBootAdminClient(); 
services.AddSingleton(_ => new CloudFoundryApplicationOptions(builder.Configuration));
services.AddSingleton(_ => new CloudFoundryServicesOptions(builder.Configuration));
services.ConfigureCloudFoundryOptions(builder.Configuration);
services.AddScoped<AppEnv>();
services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
services.AddConfigServerHealthContributor();


if (features.IsMySqlBound)
{
    services.AddMySqlHealthContributor(builder.Configuration);
}
else if (features.IsSqlServerBound)
{
    services.AddSqlServerHealthContributor(builder.Configuration);
}
services.AddDbContext<AttendeeContext>(db =>
{
    if (features.IsMySqlBound)
    {
        db.UseMySql(builder.Configuration);
        logger.LogInformation("Database Provider: MySQL");
    }
    else if (features.IsSqlServerBound)
    {
        db.UseSqlServer(builder.Configuration);
        logger.LogInformation("Database Provider: SQL Server");

    }
    else
    {
        var dbFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "users.db");
        db.UseSqlite($"DataSource={dbFile}");
        logger.LogInformation("Database Provider: SQLite");

    }
});
services.AddTask<MigrateDbContextTask<AttendeeContext>>(ServiceLifetime.Scoped);
services.AddTransient<SkipCertValidationHttpHandler>();
services.AddTransient<TasClientCertificateHttpHandler>();



var httpClientBuilder = services.AddHttpClient(Options.DefaultName)
    .ConfigurePrimaryHttpMessageHandler<TasClientCertificateHttpHandler>();


var config = builder.Configuration;
if (features.IsEurekaBound)
{
    services.AddDiscoveryClient();
    httpClientBuilder.AddServiceDiscovery();
    services.AddHttpClient<EurekaDiscoveryClient>("Eureka").ConfigurePrimaryHttpMessageHandler<SkipCertValidationHttpHandler>();
    services.PostConfigure<EurekaInstanceOptions>(c => // use for development to set instance ID and other things for simulated c2c communications
    {
        if (c.RegistrationMethod == "direct")
        {
            config.Bind("Eureka:Instance", c);
        }
    });
}

else
{
    logger.LogWarning("Service discovery (Eureka) integration disabled as no binding information was found. Discovery client will fall back to preconfigured " +
                      "services found under 'Discovery:Services' configuration key");
    services.AddConfigurationDiscoveryClient(config);
    services.AddSingleton<IDiscoveryClient, ConfigurationDiscoveryClient>();
}


services.PostConfigure<CertificateOptions>(opt =>
{
    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) && opt.Certificate != null)
    {
        try
        {
            // work around bug when running on Windows
            opt.Certificate = new X509Certificate2(opt.Certificate.Export(X509ContentType.Pkcs12));
        }
        catch (Exception)
        {
            // ignored
        }
    }
});


    

if (builder.Environment.IsDevelopment())
{
    services.AddTransient<SimulatedClientCertInHeaderHttpHandler>();
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler<SimulatedClientCertInHeaderHttpHandler>();
}



services
    .AddControllersWithViews()
    .AddRazorRuntimeCompilation();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCookiePolicy(); 
app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseAzureSpringApps();
app.UseRouting();
app.UseCloudFoundryCertificateAuth();
// app.UseAuthentication();
// app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});
app.EnsureMigrationOfContext<AttendeeContext>();
            
app.Run();
