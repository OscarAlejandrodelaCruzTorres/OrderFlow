using 'main.bicep'

param serverName = 'sql-orderflow-dev-oadt'
param adminObjectId = '7d09575b-f8c1-4f91-a68c-3e6b4896c6bb'
param adminLogin = readEnvironmentVariable('ORDERFLOW_SQL_ADMIN_LOGIN')
param clientIp = readEnvironmentVariable('ORDERFLOW_CLIENT_IP')
param databaseNames = ['OrderFlowCF_Catalog', 'OrderFlowCF_Shared']
param alertEmail = readEnvironmentVariable('ORDERFLOW_ALERT_EMAIL')