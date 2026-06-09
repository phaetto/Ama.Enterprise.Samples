@description('The location for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('The base name used for generating resource names.')
param baseName string = 'ama-enterprise-featureflags'

@description('The name of the existing App Service Plan.')
param appServicePlanName string

@description('Resource group of the existing App Service Plan.')
param appServicePlanResourceGroupName string = resourceGroup().name

@description('The name of the Web App. Unique string appended to prevent name collisions.')
param webAppName string = '${baseName}-app-${uniqueString(resourceGroup().id)}'

@description('The name of the Virtual Network.')
param vnetName string = '${baseName}-vnet'

@description('Base64 encoded cluster certificate.')
@secure()
param clusterCertificateBase64 string = ''

@description('Base64 encoded encryption key.')
@secure()
param encryptionKeyBase64 string = ''

@description('Advertised host for the node.')
param advertisedHost string = ''

@description('Advertised port for the node.')
param advertisedPort string = ''

@description('Whether to use HTTPS.')
param useHttps string = ''

@description('Target host for peer discovery.')
param targetHost string = ''

@description('Target port for peer discovery.')
param targetPort string = ''

@description('Connection string for the data storage account.')
@secure()
param dataStorageConnectionString string

// 1. Virtual Network & Subnet
resource vnet 'Microsoft.Network/virtualNetworks@2023-04-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'AppServiceSubnet'
        properties: {
          addressPrefix: '10.0.1.0/24'
          delegations: [
            {
              name: 'webapp-delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
}

// 2. Existing App Service Plan
resource existingAppServicePlan 'Microsoft.Web/serverfarms@2022-09-01' existing = {
  name: appServicePlanName
  scope: resourceGroup(appServicePlanResourceGroupName)
}

// 3. App Service (Web App)
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: existingAppServicePlan.id
    virtualNetworkSubnetId: vnet.properties.subnets[0].id
    vnetRouteAllEnabled: true 
    clientAffinityEnabled: false
    siteConfig: {
      alwaysOn: true 
      linuxFxVersion: 'DOTNETCORE|10.0' 
      http20Enabled: true
      vnetPrivatePortsCount: 1
      cors: {
        allowedOrigins: [
          '*'
        ]
      }
      appSettings: [
        {
          name: 'WEBSITE_VNET_ROUTE_ALL'
          value: '1'
        }
        {
          name: 'DataStorageConnectionString'
          value: dataStorageConnectionString
        }
        {
          name: 'ClusterCertificateBase64'
          value: clusterCertificateBase64
        }
        {
          name: 'EncryptionKeyBase64'
          value: encryptionKeyBase64
        }
        {
          name: 'AdvertisedHost'
          value: advertisedHost
        }
        {
          name: 'AdvertisedPort'
          value: advertisedPort
        }
        {
          name: 'UseHttps'
          value: useHttps
        }
        {
          name: 'TargetHost'
          value: targetHost
        }
        {
          name: 'TargetPort'
          value: targetPort
        }
      ]
    }
  }
}

output webAppUrl string = 'https://${webApp.properties.defaultHostName}'