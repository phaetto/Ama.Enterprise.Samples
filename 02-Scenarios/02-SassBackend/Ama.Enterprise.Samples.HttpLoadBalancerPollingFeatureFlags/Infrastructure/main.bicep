@description('The location for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('The base name used for generating resource names.')
param baseName string = 'ama-enterprise-featureflags'

@description('The name of the App Service Plan.')
param appServicePlanName string

@description('The SKU of the App Service Plan.')
param appServicePlanSku string = 'B1'

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

@description('Advertised port for the node.')
param advertisedPort string = ''

@description('Whether to use HTTPS.')
param useHttps string = ''

@description('Target host for peer discovery.')
param targetHost string = ''

@description('Target port for peer discovery.')
param targetPort string = ''

var storageAccountName = 'st${uniqueString(resourceGroup().id, baseName)}'

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

// 2. App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux App Service Plans
  }
}

// 3. Storage Account for Data
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource featureFlagsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  parent: tableService
  name: 'FeatureFlagsDistributedCrdtStorage'
}

var dataStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

// 4. App Service (Web App)
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
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