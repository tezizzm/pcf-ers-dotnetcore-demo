# Cloud Platform Demo for .NET
This is a demo application that shows off how applications instrumented with Steeltoe can benefit from running on modern cloud platforms. The following platforms are currently supported

- Tanzu Application Service
- Azure Spring Apps Enterprise

## Introduction
This application is interactive and has instructions on each demo page. It shows off the following features:

|                                                              | TAS  | ASA  |
| ------------------------------------------------------------ | ---- | ---- |
| General environmental information (app name, IP, CLR version, environment) | ✓    | ✓    |
| Service bindings information                                 | ✓    | ✓*   |
| Connectors (MySQL, SQLServer)                                | ✓    |      |
| Actuators                                                    | ✓    | ✓**  |
| Blue/Green deployments                                       | ✓    | ✓    |
| Service Discovery /w Eureka                                  | ✓    | ✓    |
| **Config**                                                   |      |      |
| Config Server integration                                    | ✓    |      |
| Config Service integration                                   |      | ✓    |
| Placeholder configuration provider                           | ✓    | ✓    |
| Random configuration provider                                | ✓    |      |
| **Security**                                                 |      |      |
| App-to-app mTLS with container identity certificate          | ✓    |      |
| Single Signon                                                | ✓    |      |

*ASA only injects Application Insights information via standard [Kubernetes Service Binding](https://servicebinding.io/) specification 
**ASA doesn't have LiveView support yet for .NET apps, but actuators endpoints can be viewed manually

The app can be deployed without any dependencies, but some demos require additional services to work. Instructions are embedded into the app itself.

## Getting Started

### Build automation script

The app comes with build automation scripts that will help do a number of build targets (aka tasks). Each target, can be invoked by running `build.ps1` or `build.sh` followed by target name and optional parameters. Example:

```
.\build.sh Publish
```

All available targets are available by running `\build.ps1` with no args

### Included targets

- `Publish` - compile the app targeting `linux-x64` - suitable for deployment to TAS. The manifest in root of the repo already points to folder where output of publish command is placed. 

- `Pack` - packages output of `Publish` command as versioned zip file inside `/artifacts` folder

- `CfDeploy` - deploys to current TAS with the following features: 

  - 3 copies of app `cpdemo-blue`, `cpdemo-green`, and `cpdemo-backend`. Blue/green will be assigned public routes via default domain, while `ers-backend` will be mapped to an internal (container-to-container) domain.
  - Allow blue/green apps to talk c2c to `ers-backend` on port `8433`. `ers-backend` uses CF container identity cert for port 8433
  - If available in marketplace, create and bind to all apps the following services: mysql, eureka, sso

  Use this target to automate deployment of full demo to TAS

- `AzureDeploy` - deploys app to Azure Spring Apps Enterprise. If deployment name not specifies, deploys twice into `green` and `blue` deployments

## Tanzu Application Service

### Deploying with build script

Ensure your `cf` cli is targeting org and space where you want the app to be deployed

```
.\build.sh CfDeploy
```

### Deploying manually

Basic cf push can be done as following. Note that it doesn't include all the services necessary for all demos

```
.\build.ps1 Publish
cf push
```




### SSO Demo

To demo SSO, you need to setup SSO plan to show up in marketplace. If you only have a single plan, it will be automatically determined, otherwise use `--sso-plan` argument to specify which plan to use. The demo configures each app to use an identity provider for the plan called `gcp` for demoing using SSO tile to integrate with Google as identity provider. You can change the name of the SSO identity provider to use via `sso-binding.json` file. 

## Inner loop demo

You can do a TAP style inner-loop workflow using provided Tilt configuration. Changes to local system are automatically synchronized to remote container by copying over delta files and restarting the process without doing a full "push", greatly speeding up the feedback cycle. First time always results in a full push

Start by executing `build.ps1 --sync-trigger Source` to synchronize with remote instance whenever local source code changes.

Alternatively, use  `build.ps1 --sync-trigger Build` to synchronize with remote instance whenever a local build is triggered

## Azure Spring Apps

The demo was well on Azure Spring Apps. Due to lack of LiveView for non Spring Apps, there's no platform visualizer for actuator endpoints

You can deploy to Azure via an automated build script using the following command

```
build.sh AzureDeploy --asa-service-name SERVICE_NAME --asa-resource-group RESOURCE_GROUP
```

It will automate configuration of the service and necessary components

### Deploying to Azure manually

1. Set default service and resource name

   ```
   az configure --defaults group={RESOURCE_GROUP} spring={ASA_SERVICE_NAME}
   ```

2. Enable config service and add git source. Pattern is used to select while config files to pull in

   ```
   spring application-configuration-service create
   spring application-configuration-service git repo add --name {AppName} --patterns cpd,cpd-blue --uri {GitRepoUri} --label master --search-paths config
   ```

3. Enable eureka service discovery

   ```
   spring service-registry create
   ```

3. Create the app with 2 deployments

   ```
   spring app create -n {AppName} --deployment-name green
   spring app deployment create --config-file-patterns {ConfigServicePattern} --app {AppName} -n blue
   ```

4. Bind the app to config service and eureka

   ```
   spring service-registry bind --app {AppName}
   spring application-configuration-service bind --app {AppName}
   ```

5. Deploy the app into the two deployment slots

   ```
   spring app deploy -n {AppName} --artifact-path {ArtifactsDirectory / PackageZipName} --config-file-pattern {AppName} --deployment blue
   spring app deploy -n {AppName} --artifact-path {ArtifactsDirectory / PackageZipName} --config-file-pattern {AppName} --deployment green
   ```

   

