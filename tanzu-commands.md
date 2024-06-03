## Commands

### Install Tanzu CLI

https://github.com/vmware-tanzu/tanzu-cli/blob/main/docs/quickstart/install.md#apt-debianubuntu

```
tanzu plugin install --group vmware-tanzu/platform-engineer
tanzu plugin install --group vmware-tanzu/app-developer
```

### Target equivalent of  "foundation"

```
tanzu context create
```

### Target equivalent of "org"

```
tanzu project list
tanzu project use AMER-East
```

### Target equivalent of "space"

```
tanzu space list
tanzu space use andrew-cpd
```

### Create build config that points to registry

```
tanzu build config --build-plan-source-type=ucp --containerapp-registry docker.io/macsux/{name}
```

### New project

```
tanzu app init

```

### Deploy

```

tanzu deploy
```

### Services

```
tanzu service type list
tanzu service create MySQLInstance/<name>
tanzu service bind MySQLInstance/mysql ContainerApp/cloud-platform-demo --as mysql
```

