## Unable to consume service binding information from .NET app running on ASA.

One of the value of ASA is the configuration management via config server and service discovery via eureka which are automatically deployed in every ASA cluster. The information on how applications can connect to them should be injected into the app when an app is bound to an instance of eureka or config server. At the moment, this binding does not result in any of this information being injected for .NET apps neither as environmental variables nor files on disk.

### Test scenario

A [sample .NET app](https://github.com/macsux/pcf-ers-dotnetcore-demo) instrumented with Steeltoe and `Microsoft.Azure.SpringCloud.Client` nuget package was deployed to ASA. The app was bound to instance of eureka.

#### Expected behavior

App discovers eureka and applies necessary binding configuration to bootstrap eureka client

#### Actual behavior

No binding information is present in the running container

### Additional analysis

A demo Spring boot application was deployed and bound to the same eureka instance. Both .NET and Spring application containers were then examined for environmental variables and list of files injected into the container. Spring boot app had `JAVA_OPTS` env var that looked like this present: `Deureka.client.service-url.defaultZone=``https://asae.svc.azuremicroservices.io/eureka/default/eureka`` -Dcom.sun.management.jmxremote -Dcom.sun.management.jmxremote.port=1099 -Dcom.sun.management.jmxremote.local.only=true -Dmanagement.endpoints.jmx.exposure.include=health,metrics -Dcom.sun.management.jmxremote.authenticate=false -Dcom.sun.management.jmxremote.ssl=false -Dspring.jmx.enabled=true -Dserver.tomcat.mbeanregistry.enabled=true -Dfile.encoding=UTF8 -Dspring.config.import=optional:configserver:/` 

This seems to be the only place where the binding information is injected. A similar environmental variable was not present in .NET container. Additionally, the .NET package `Microsoft.Azure.SpringCloud.Client` attempts to locate binding information in location where `SPRING_CONFIG_ADDITIONAL_LOCATION`. This variable was only set on Java container, but the properties file present in that location is empty in both java and .net. 

## Recommendation

Steeltoe already implements support for [Service Binding for Kubernetes](https://servicebinding.io/spec/core/1.0.0/) and ASA already seems to implement it as there's a binding injected into every container for application insights such as this structure. Any service bindings for config server and eureka should be injected in this format to allow uniform way of consuming service bindings in other language stacks. 

```
/bindings>
.
  |-07default-07default-07default-1
  |  |-connection_string
  |  |-type
  |  |-sampling_percentage
```

