using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Articulate;
using CloudPlatformDemo;
using CloudPlatformDemo.Models;
using CloudPlatformDemo.Repositories;
using CloudPlatformDemo.Utils;
using CloudPlatformDemo.Workaround;
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
using Steeltoe.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.Kubernetes.ServiceBinding;
using LocalCertificateWriter = CloudPlatformDemo.LocalCerts.LocalCertificateWriter;

var logger = LoggerFactory.Create(c => c
    .AddSimpleConsole(f => f
        .SingleLine = true))
    .CreateLogger<Program>();
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;


// when running locally, get config from <gitroot>/config folder
// string configDir = "../config/";
// var appName = typeof(Program).Assembly.GetName().Name;
builder.Configuration
    .AddYamlFile("appsettings.yaml", false, true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", true, true)
    .AddProfiles();
    
if (Platform2.IsAzureSpringApps)
{
    builder.AddAzureSpringApp();
    
    // these line is a hack for a bug that overrides use of ClientCertificateHttpHandler with one implemented in this demo. Should be backported into steeltoe
    builder.Services
        .AddSingleton<IHttpClientHandlerProvider, TempFixHttpClientHandlerProvider>()
        .AddSingleton<ClientCertificateHttpHandler2>();
}

if (Platform.IsCloudFoundry)
{
    builder.Services.RegisterDefaultApplicationInstanceInfo();
    builder.Services.AddOptions<ApplicationInstanceInfo>().Configure<IApplicationInstanceInfo>((opt, global) =>
    {
        opt.Name = global.ApplicationName;
        opt.Instance_Id = global.InstanceId;
    }); // a little hack to reregister ApplicationInstanceInfo as options vs singleton that steeltoe does by default. we only use a couple of properties from this class so no need to copy everything
    builder.UseCloudFoundryCertificateForInternalRoutes();
    builder.Configuration
        .AddCloudFoundry()
        .AddCloudFoundryContainerIdentity();
    if (builder.Environment.IsDevelopment())
    {
        builder.UseDevCertificate();
    }
    
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
    services.ConfigureOptions(typeof(CloudFoundryServiceConfigureOptions));
}

builder.Configuration
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var envInfo = new EnvironmentInfo(builder.Configuration);
builder.Services.AddSingleton(envInfo);
if (envInfo.IsConfigServerBound)
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


if (Platform2.IsAzureSpringApps)
{
    // var applicationInfo = new ApplicationInstanceInfo
    // {
    //     Name = builder.Configuration.GetValue<string>("AZURE_SPRING_APPS:APP:NAME")
    // };
    // services.Configure<ApplicationInstanceInfo>(c =>
    // {
    //     c.Name = builder.Configuration.GetValue<string>("AZURE_SPRING_APPS:APP:NAME");
    // });
    //services.Configure<KubernetesServicesOptions>(builder.Configuration);
    services.ConfigureOptions(typeof(KubernetesServiceConfigureOptions));
}




services.AddDistributedTracing();

//services.AddSpringBootAdminClient(); 

services.ConfigureCloudFoundryOptions(builder.Configuration);
services.AddScoped<AppEnv>();
services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
services.AddConfigServerHealthContributor();


if (envInfo.IsMySqlBound)
{
    services.AddMySqlHealthContributor(builder.Configuration);
}
else if (envInfo.IsSqlServerBound)
{
    services.AddSqlServerHealthContributor(builder.Configuration);
}
services.AddDbContext<AttendeeContext>(db =>
{
    if (envInfo.IsMySqlBound)
    {
        db.UseMySql(builder.Configuration);
        logger.LogInformation("Database Provider: MySQL");
    }
    else if (envInfo.IsSqlServerBound)
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
var httpClientBuilder = services.AddHttpClient(Options.DefaultName)
    .ConfigurePrimaryHttpMessageHandler<SkipCertValidationHttpHandler>();


var config = builder.Configuration;
if (envInfo.IsEurekaBound)
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


// services.PostConfigure<CertificateOptions>(opt =>
// {
//     // if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) && opt.Certificate != null)
//     // {
//         try
//         {
//             // work around bug when running on Windows
//             opt.Certificate = new X509Certificate2(config.GetValue<string>("certificate"), (string)null, X509KeyStorageFlags.Exportable);
//             //opt.Certificate = new X509Certificate2(opt.Certificate.Export(X509ContentType.Pkcs12));
//         }
//         catch (Exception)
//         {
//             // ignored
//         }
//     // }
// });


    

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
