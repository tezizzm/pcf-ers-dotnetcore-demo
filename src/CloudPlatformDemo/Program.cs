using System.Reflection;
using CloudPlatformDemo.Models;
using CloudPlatformDemo.Repositories;
using CloudPlatformDemo.Workaround;
using EasyNetQ;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Azure.SpringCloud.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Logging;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Configuration.RandomValue;
using Steeltoe.Connectors.EntityFrameworkCore;
using Steeltoe.Connectors.EntityFrameworkCore.MySql;
using Steeltoe.Connectors.EntityFrameworkCore.SqlServer;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.RabbitMQ;
using Steeltoe.Connectors.SqlServer;
// using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Tracing;
using Steeltoe.Security.Authentication.CloudFoundry;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.HttpClients;
using Steeltoe.Management.Task;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: SystemConsoleTheme.Colored)
    .CreateLogger();

// var logger = LoggerFactory.Create(c => c
//     .AddSimpleConsole(f => f
//         .SingleLine = true))
//     .CreateLogger<Program>();
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.Configuration
    .AddYamlFile("appsettings.yaml", false, true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", true, true)
    .AddProfiles();
if (Platform2.IsTanzuApplicationPlatform)
{
    Log.Information("Adding Tanzu Application Platform bits");
    builder.Configuration.AddKubernetesServiceBindings();
}
if (Platform2.IsAzureSpringApps)
{
    Log.Information("Adding ASA bits");
    builder.AddAzureSpringApp();
    builder.Services.AddEurekaDiscoveryClient();

}
if (Platform2.IsKubernetes)
{
    Log.Information("Adding Kubernetes bits");
    services.ConfigureOptions(typeof(KubernetesServiceConfigureOptions));
}
if (Platform.IsCloudFoundry)
{
    Log.Information("Adding Cloud Foundry bits");
    builder.Services.RegisterCloudFoundryApplicationInstanceInfo();
    if (Environment.GetEnvironmentVariable("VCAP_SERVICES") != null)
    {
        builder.Configuration.AddCloudFoundryServiceBindings();
    }
    else
    {
        // in local development, it's easier to manage values normally supplied by vcap_services env var in a yaml file
        builder.Configuration.AddCloudFoundryServiceBindings(_ => false, new YamlServiceBindingsReader("vcap_services.yaml"), BootstrapLoggerFactory.Default);
    }

    builder.Configuration
        .AddCloudFoundry()
        .AddCloudFoundryContainerIdentity();

    services.AddCloudFoundryContainerIdentity();
    services.AddAuthentication((options) =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CloudFoundryDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
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
else
{
    builder.Services.RegisterDefaultApplicationInstanceInfo();
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
    Log.Warning("Config server not being used as no binding information was present");
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
services.AddDistributedTracing();

//services.AddSpringBootAdminClient(); 

services.ConfigureCloudFoundryOptions(builder.Configuration);
services.AddScoped<AppEnv>();
services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
services.AddConfigServerHealthContributor();

if (envInfo.IsRabbitMqBound)
{
    builder.AddRabbitMQ();
    builder.Services.RegisterEasyNetQ(svc =>
    {
        var connectionFactory = svc.Resolve<ConnectionFactory>();
        var host = new HostConfiguration
        {
            Host = connectionFactory.HostName ?? connectionFactory.VirtualHost,
            Port = (ushort)connectionFactory.Port
        };
        // use reflection cuz EasyNetQ made SSL property readonly and we don't wanna to copy all the fields one by one - just reflectively set the whole object to the backing field
        var sslField = host.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance).OfType<FieldInfo>().FirstOrDefault(x => x.FieldType == typeof(SslOption));
        if (sslField != null)
        {
            sslField.SetValue(host, connectionFactory.Ssl);
        }

        return new ConnectionConfiguration
        {
            Hosts = new List<HostConfiguration> { host },
            UserName = connectionFactory.UserName,
            Password = connectionFactory.Password
        };
    });
}

if (envInfo.IsMySqlBound)
{
    builder.AddMySql();
    services.AddDbContext<AttendeeContext>((serviceProvider, db) => db.UseMySql(serviceProvider));
    Log.Information("Database Provider: MySQL");
}
else if (envInfo.IsSqlServerBound)
{
    services.AddSqlServer(builder.Configuration);
    services.AddDbContext<AttendeeContext>((serviceProvider, db) => db.UseSqlServer(serviceProvider));
    Log.Information("Database Provider: SQL Server");
}
else
{
    var dbDir = Directory.Exists("/tmp") ? "/tmp" : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    var dbFile =  Path.Combine(dbDir, "users.db");
    services.AddDbContext<AttendeeContext>(db => db.UseSqlite($"DataSource={dbFile}"));
    Log.Information("Database Provider: SQLite");
}

services.AddTask<MigrateDbContextTask<AttendeeContext>>(ServiceLifetime.Scoped);
var httpClientBuilder = services.AddHttpClient(Options.DefaultName);

if (envInfo.IsEurekaBound)
{
    Log.Information("Adding Eureka");
    builder.Services.AddEurekaDiscoveryClient();
    httpClientBuilder.AddServiceDiscovery();
    if (Platform2.IsAzureSpringApps)
    {
        Log.Information("Configuring eureka to use client side certs with ASA");
        services.AddTransient<ClientCertificateHttpHandler2>();
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler<ClientCertificateHttpHandler2>();
    }
    else
    {
        services.AddHttpClient("Eureka").ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler());
    }
}

else
{
    Log.Warning("Service discovery (Eureka) integration disabled as no binding information was found. Discovery client will fall back to preconfigured " +
                      "services found under 'Discovery:Services' configuration key");
    services.AddConfigurationDiscoveryClient();
    services.AddSingleton<IDiscoveryClient, ConfigurationDiscoveryClient>();
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

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
app.EnsureMigrationOfContext<AttendeeContext>();

app.Run();
