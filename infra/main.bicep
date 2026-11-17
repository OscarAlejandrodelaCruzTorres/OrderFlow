param serverName string
param adminLogin string
param adminObjectId string
param clientIp string
param location string = resourceGroup().location

module sql 'modules/sql.bicep' = {
  params: {
    serverName: serverName
    location: location
    adminLogin: adminLogin
    adminObjectId: adminObjectId
    clientIp: clientIp
  }
}

output serverFqdn string = sql.outputs.serverFqdn