@description('Globally unique: it becomes <name>.database.windows.net.')
param serverName string

@description('Region of the resources. Defaults to the resource group\'s.')
param location string = resourceGroup().location

@description('Display name of the Entra admin (your UPN).')
param adminLogin string

@description('Object id of the Entra admin: the real key Azure SQL uses.')
param adminObjectId string

@description('Public IP allowed through the firewall. Passed at deploy time, never committed.')
param clientIp string

@description('One free-tier database per name. The free offer is per database, up to 10 per subscription.')
param databaseNames array

resource server 'Microsoft.Sql/servers@2025-01-01' = {
  name: serverName
  location: location
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: adminLogin
      sid: adminObjectId
      tenantId: tenant().tenantId
      principalType: 'User'
    }
  }
}

resource clientFirewallRule 'Microsoft.Sql/servers/firewallRules@2025-01-01' = {
  parent: server
  name: 'AllowClientIp'
  properties: {
    startIpAddress: clientIp
    endIpAddress: clientIp
  }
}

resource databases 'Microsoft.Sql/servers/databases@2025-01-01' = [
  for name in databaseNames: {
    parent: server
    name: name
    location: location
    sku: {
      name: 'GP_S_Gen5'
      tier: 'GeneralPurpose'
      family: 'Gen5'
      capacity: 2
    }
    properties: {
      useFreeLimit: true
      freeLimitExhaustionBehavior: 'AutoPause'
    }
  }
]

output serverFqdn string = server.properties.fullyQualifiedDomainName
output databaseIds array = [for (name, i) in databaseNames: databases[i].id]
