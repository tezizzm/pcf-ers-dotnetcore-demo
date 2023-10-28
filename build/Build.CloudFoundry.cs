using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.CloudFoundry;
using Serilog;
using static Tools.CloudFoundry.CloudFoundryExtensions;
using static Nuke.Common.Tools.CloudFoundry.CloudFoundryTasks;

public partial class Build
{
    [Parameter("Cloud Foundry Username")]
    readonly string CfUsername;
    [Parameter("Cloud Foundry Password")]
    readonly string CfPassword;
    [Parameter("Cloud Foundry Endpoint")]
    readonly string CfApiEndpoint;
    [Parameter("Cloud Foundry Org")]
    string CfOrg;
    [Parameter("Cloud Foundry Space")]
    string CfSpace;
    [Parameter("App Name for inner loop")]
    string AppName = "ers1";
    [Parameter("Type of database plan (default: db-small)")]
    readonly string DbPlan = "db-small";
    [Parameter("Type of SSO plan")]
    string SsoPlan;
    [Parameter("Internal domain for c2c. Optional")]
    string InternalDomain = null;
    [Parameter("Public domain to assign to apps. Optional")]
    string PublicDomain;
    [Parameter("Trigger to use to trigger live sync (Build or Source. Default to Source)")]
    SyncTrigger SyncTrigger = SyncTrigger.Source;
    [Parameter("Should CF push be performed when livesync is started. Disabling is quicker, but only works if app was previously deployed for livesync")]
    bool CfPushInit = true;
    
    Target CfLogin => _ => _
        // .OnlyWhenStatic(() => !CfSkipLogin)
        .Requires(() => CfUsername, () => CfPassword, () => CfApiEndpoint)
        .Unlisted()
        .Executes(() =>
        {
            CloudFoundryApi(c => c.SetUrl(CfApiEndpoint));
            CloudFoundryAuth(c => c
                .SetUsername(CfUsername)
                .SetPassword(CfPassword));
        });

    Target CfTarget => _ => _
        .Requires(() => CfSpace, () => CfOrg)
        .Executes(() =>
        {
            CloudFoundryCreateSpace(c => c
                .SetOrg(CfOrg)
                .SetSpace(CfSpace));
            CloudFoundryTarget(c => c
                .SetSpace(CfSpace)
                .SetOrg(CfOrg));
        });

    Target InnerLoop => _ => _
        .Requires(() => AppName)
        .Executes(async () =>
        {
            var currentEnvVars = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .ToDictionary(x => (string)x.Key, x => (string)x.Value);
            string os = "";
            if (OperatingSystem.IsWindows())
                os = "win";
            else if (OperatingSystem.IsLinux())
                os = "linux";
            else
                os = "osx";
            var tilt = ToolPathResolver.GetPackageExecutable($"Tilt.CommandLine.{os}-x64", "tilt" + (OperatingSystem.IsWindows() ? ".exe" : ""));
            var tiltProcess = ProcessTasks.StartProcess(tilt, "up", 
                workingDirectory: RootDirectory, 
                environmentVariables: new Dictionary<string, string>(currentEnvVars)
                {
                    {"APP_NAME", AppName},
                    {"APP_DIR", "./src"},
                    {"SYNC_TRIGGER", SyncTrigger.ToString().ToLower()},
                    {"CF_PUSH_INIT", CfPushInit.ToString().ToLower()},
                    {"AssemblyName", AssemblyName},
                    // {"PUSH_PATH", "."},
                    // {"PUSH_COMMAND", $"cd ${{HOME}} && ./watchexec --ignore *.yaml --restart --watch . 'dotnet {AssemblyName}.dll --urls http://0.0.0.0:8080'"},
                    {"TILT_WATCH_WINDOWS_BUFFER_SIZE", "555555"}
                });
            await Task.Delay(3000);
            var tiltPsi = new ProcessStartInfo
            {
                FileName = "http://localhost:10350",
                UseShellExecute = true
            };
            Process.Start(tiltPsi);
            
            tiltProcess.WaitForExit();
        });

    struct AppDeployment
    {
        public string Name { get; set; }
        public string Org { get; set; }
        public string Space { get; set; }
        public string Domain { get; set; }
        public string Hostname => $"{Name}-{Space}-{Org}";
        public bool IsInternal { get; set; }
    }

    Target DeployFull => _ => _
        .DependsOn(CfLogin, CfTarget, CfDeploy)
        .Executes(() =>
        {
            
        });


    Target EnsureCfCurrentTarget => _ => _
        .After(CfTarget, Pack)
        .Executes(() =>
        {
            var userProfileDir = (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var cfConfigFile = userProfileDir / ".cf" / "config.json";
            var cfConfig = JObject.Parse(File.ReadAllText(cfConfigFile));

            CfOrg = cfConfig.SelectToken($"OrganizationFields.Name")?.Value<string>(); 
            CfSpace = cfConfig.SelectToken($"SpaceFields.Name")?.Value<string>();
            if (CfOrg is null || CfSpace is null)
            {
                Assert.Fail("CF CLI is not set to an org/space");
            }
            var orgGuid = CloudFoundry($"org {CfOrg} --guid", logOutput: false).StdToText();
            PublicDomain = CloudFoundryCurl(o => o
                    .SetPath($"/v3/organizations/{orgGuid}/domains/default")
                    .DisableProcessLogOutput())
                .StdToJson<dynamic>().name;
            InternalDomain = CloudFoundryCurl(o => o
                    .SetPath($"/v3/domains")
                    .DisableProcessLogOutput())
                .ReadPaged<dynamic>()
                .Where(x => x.@internal == true)
                .Select(x => x.name)
                .First();
            
            Assert.True(PublicDomain != null);
            Assert.True(InternalDomain != null);
        });

    Target CreateDeploymentPlan => _ => _
        .Unlisted()
        .DependsOn(EnsureCfCurrentTarget)
        .Executes(() =>
        {
            Green = new AppDeployment
            {
                Name = "ers-green",
                Org = CfOrg,
                Space = CfSpace,
                Domain = PublicDomain
            };
            Blue = Green;
            Blue.Name = "ers-blue";

            Backend = Green;
            Backend.Name = "ers-backend";
            Backend.Domain = InternalDomain;
            Backend.IsInternal = true;
            Apps = new[] { Green, Blue, Backend };
        });
    Target CfDeploy => _ => _
        .After(CfLogin, CfTarget)
        .DependsOn(Pack, EnsureCfCurrentTarget, CreateDeploymentPlan)
        .Description("Deploys {AppsCount} instances to Cloud Foundry /w all dependency services into current target set by cli")
        .Executes(async () =>
        {

            var marketplace = CloudFoundry("marketplace", logOutput: false).StdToText();
            var hasMySql = marketplace.Contains("p.mysql");
            var hasDiscovery = marketplace.Contains("p.service-registry");
            var hasSso = marketplace.Contains("p-identity");
            if (hasSso && SsoPlan == null)
            {
                SsoPlan = Regex.Match(marketplace, @"(?<=^p-identity\s+)[^\s]+", RegexOptions.Multiline).Value;
            }

            if (hasDiscovery)
            {
                CloudFoundryCreateService(c => c
                    .SetService("p.service-registry")
                    .SetPlan("standard")
                    .SetInstanceName("eureka"));
            }
            else
            {
                Log.Warning("Service registry not detected in marketplace. Some demos will not be available");
            }

            if (hasMySql)
            {
                CloudFoundryCreateService(c => c
                    .SetService("p.mysql")
                    .SetPlan(DbPlan)
                    .SetInstanceName("mysql"));
            }
            else
            {
                Log.Warning("MySQL not detected in marketplace. Some demos will not be available");
            }

            if (hasSso)
            {
                CloudFoundryCreateService(c => c
                    .SetService("p-identity")
                    .SetPlan(SsoPlan)
                    .SetInstanceName("sso"));
            }
            else
            {
                Log.Warning("SSO not detected in marketplace. Some demos will not be available");
            }
            
            CloudFoundryPush(c => c
                .EnableNoRoute()
                .EnableNoStart()
                .SetMemory("384M")
                .SetPath(ArtifactsDirectory / PackageZipName)
                .CombineWith(Apps,(push,app) =>
                {
                    
                    push = push
                        .SetAppName(app.Name);
                    if (app.IsInternal) // override start command as buildpack sets --urls flag which prevents us from binding to non standard ssl port
                    {
                        push = push.SetStartCommand($"cd ${{HOME}} && ASPNETCORE_URLS='http://0.0.0.0:8080;https://0.0.0.0:8443' && exec ./{AssemblyName}");
                    }
                    return push;
                }), degreeOfParallelism: 3);

            // bind backend to both regular 8080 http port and 8443 which can be accessed directly by other apps bypassing gorouter
            CloudFoundrySetEnv(c => c
                .SetAppName(Backend.Name)
                .SetEnvVarName("ASPNETCORE_URLS")
                .SetEnvVarValue("http://0.0.0.0:8080;https://0.0.0.0:8443")); 
            
            CloudFoundrySetEnv(c => c
                .SetAppName(Backend.Name)
                .SetEnvVarName("SPRING__PROFILES__ACTIVE")
                .SetEnvVarValue("Backend")); 
            
            // CloudFoundryPush(c => c
            //     .SetAppName(backend)
            //     .EnableNoRoute()
            //     .EnableNoStart()
            //     .SetPath(ArtifactsDirectory / PackageZipName)
            //     .SetProcessEnvironmentVariable("ASPNETCORE_URLS", "http://0.0.0.0:8080;https://0.0.0.0:8443"));
            

            // CloudFoundryMapRoute(c => c
            //     .SetDomain(defaultDomain)
            //     .CombineWith(names, (cfg, app) => cfg
            //         .SetAppName(app)
            //         .SetHostname($"{app}-{CfSpace}-{CfOrg}"))); 
            
            CloudFoundryMapRoute(c => c
                .CombineWith(Apps, (cf,app) => cf
                    .SetAppName(app.Name)
                    .SetDomain(app.Domain)
                    .SetHostname(app.Hostname))
                , degreeOfParallelism: 3);

            CloudFoundry($"add-network-policy {Green.Name} {Backend.Name} --port 8443 --protocol tcp"); // expose on ssl as well
            CloudFoundry($"add-network-policy {Blue.Name} {Backend.Name} --port 8443 --protocol tcp"); // expose on ssl as well
            
            await CloudFoundryEnsureServiceReady("eureka");
            await CloudFoundryEnsureServiceReady("mysql");
            await CloudFoundryEnsureServiceReady("sso");

            
            CloudFoundryBindService(c => c
                .SetServiceInstance("eureka")
                .CombineWith(Apps, (cf,app) => cf
                    .SetAppName(app.Name)), degreeOfParallelism: 3);

            CloudFoundryBindService(c => c
                .SetServiceInstance("mysql")
                .CombineWith(Apps, (cf, app) => cf
                    .SetAppName(app.Name)), degreeOfParallelism: 3);

            CloudFoundryBindService(c => c
                .SetServiceInstance("sso")
                .SetConfigurationParameters(RootDirectory / "sso-binding.json")
                .CombineWith(Apps, (cf, app) => cf
                    .SetAppName(app.Name)), degreeOfParallelism: 3);
            

            CloudFoundryStart(c => c
                .CombineWith(Apps, (cf, app) => cf
                    .SetAppName(app.Name)), degreeOfParallelism: 3);
        });

    Target DeleteApps => _ => _
        .After(CfLogin, CfTarget)
        .DependsOn(EnsureCfCurrentTarget, CreateDeploymentPlan)
        .Executes(() =>
        {
            CloudFoundryDeleteApplication(c => c
                .EnableDeleteRoutes()
                .CombineWith(Apps, (cf, app) => cf
                    .SetAppName(app.Name)));
        });
}