param serverName string
param adminLogin string
param adminObjectId string
param clientIp string
// southcentralus, not eastus: eastus (and southcentralus too, while the subscription was a free trial)
// returned ProvisioningDisabled for Azure SQL. The first free-tier database also pins the region for
// every free database in the subscription, so this is the region from now on.
param location string = 'southcentralus'
param databaseNames array
param alertEmail string

module sql 'modules/sql.bicep' = {
  params: {
    serverName: serverName
    location: location
    adminLogin: adminLogin
    adminObjectId: adminObjectId
    clientIp: clientIp
    databaseNames: databaseNames
  }
}

module alerts 'modules/alerts.bicep' = {
  params: {
    alertEmail: alertEmail
    databaseIds: sql.outputs.databaseIds // ← la salida del módulo sql
  }
}

output serverFqdn string = sql.outputs.serverFqdn
