These configuration files are served by Spring Cloud Config Server when application is configured to use one. File naming convention is used to dictate which files are served based on application name. Application name is determined via `Spring:Application:Name` configuration key within the app.  
File matching rules are as following:
- `application.yaml` - applicable to all apps
- `<APP_NAME>.yaml` - configuration specific to an app matched on `Spring:Application:Name` config key
- `<APP_NAME>-<ENVIRONMENT>.yaml` - an environment specific overrides values per app. Environment is controlled via `ASPNETCORE_ENVIRONMENT` environmental variable for .NET apps

Note that values from all these sources get combined, with more specific ones overriding values in less specific yaml files