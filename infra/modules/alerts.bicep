@description('Receives the cost alerts. Passed at deploy time, never committed.')
param alertEmail string

@description('Ids of the free-tier databases to watch.')
param databaseIds array

@description('Alert when fewer vCore-seconds than this remain this month: 20% of the 100,000 free allowance.')
param freeAmountThreshold int = 20000

resource costActionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-orderflow-dev-cost'
  location: 'global'
  properties: {
    groupShortName: 'ofcost'
    enabled: true
    emailReceivers: [
      {
        name: 'owner'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

resource freeAmountAlerts 'Microsoft.Insights/metricAlerts@2026-01-01' = [
  for databaseId in databaseIds: {
    name: 'alert-free-amount-${last(split(databaseId, '/'))}'
    location: 'global'
    properties: {
      severity: 2
      enabled: true
      scopes: [databaseId]
      evaluationFrequency: 'PT1H'
      windowSize: 'PT6H'
      criteria: {
        'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
        allOf: [
          {
            name: 'FreeAmountLow'
            criterionType: 'StaticThresholdCriterion'
            metricName: 'free_amount_remaining'
            operator: 'LessThan'
            threshold: freeAmountThreshold
            timeAggregation: 'Minimum'
          }
        ]
      }
      actions: [
        { actionGroupId: costActionGroup.id }
      ]
    }
  }
]
