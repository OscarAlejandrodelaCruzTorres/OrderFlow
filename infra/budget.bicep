targetScope = 'subscription'

@description('Receives the budget notifications. Passed at deploy time, never committed.')
param alertEmail string

@description('Monthly budget in USD.')
param amount int = 10

@description('First day of a month (yyyy-MM-01). Fixed on purpose: a budget\'s start date shouldn\'t move on every redeploy.')
param startDate string = '2026-11-01'

resource budget 'Microsoft.Consumption/budgets@2026-06-01' = {
  name: 'budget-orderflow-dev'
  properties: {
    category: 'Cost'
    amount: amount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: startDate
    }
    notifications: {
      actualAt80Percent: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 80
        thresholdType: 'Actual'           
        contactEmails: [ alertEmail ]
      }
      forecastAt100Percent: {
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: 100
        thresholdType: 'Forecasted'       
        contactEmails: [ alertEmail ]
      }
    }
  }
}