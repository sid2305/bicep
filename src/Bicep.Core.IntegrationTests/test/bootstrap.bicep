// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
/*
  SUMMARY : Deploys the Management Groups and Subscriptions for the Sovereign Landing Zone
  AUTHOR/S: Cloud for Sovereignty
*/
targetScope = 'tenant'

@description('The prefix that will be added to all resources created by this deployment.')
@minLength(2)
@maxLength(5)
param parDeploymentPrefix string = 'mcfs'

@description('The suffix that will be added to management group suffix name the same way to be added to management group prefix names.')
@maxLength(5)
param parDeploymentSuffix string = 'test'

@description('The name of the top level management group.')
@minLength(2)
param parTopLevelManagementGroupName string = 'TestMG'

@description('The full resource ID of billing scope associated to the EA, MCA or MPA account you wish to create the subscription in.')
param parSubscriptionBillingScope string = '/providers/Microsoft.Billing/BillingAccounts/9077875/enrollmentAccounts/325054'

@description('Subscription ID for management group.')
param parManagementSubscriptionId string = ''

@description('Subscription ID for identity group.')
param parIdentitySubscriptionId string = ''

@description('Subscription ID for connectivity group.')
param parConnectivitySubscriptionId string = ''

@description('Tags to be added to deployed resources')
param parTags object = {'yrdy': 'yrtyf'
}

@description('Array to allow additional or different child Management Groups of Landing Zones Management Group to be deployed. Default: Empty Object')
param parLandingZoneMgChildren array = [{id: 'Corp'
  displayName: 'Corp'},{id: 'Online'
  displayName: 'Online'},{id: 'Confidential-Corp'
  displayName: 'Confidential Corp'},{id: 'Confidential-Online'
  displayName: 'Confidential Online'}]

@description('Optional parent for Management Group hierarchy, used as intermediate root Management Group parent, if specified. If empty, default, will deploy beneath Tenant Root Management Group.')
param parTopLevelManagementGroupParentId string = ''

var varLandingZoneMgChildren = reduce(parLandingZoneMgChildren, {}, (prev, cur) => union(prev, { '${cur.id}': cur }))

var varPlatformMgChildren = {
  management: {
    displayName: 'Management'
  }
  connectivity: {
    displayName: 'Connectivity'
  }
  identity: {
    displayName: 'Identity'
  }
}

//This module deploys management groups and creates a hierarchy of management groups
module modManagementGroups 'bootstrapDependencies/managementGroups.bicep' = {
  name: take('${parDeploymentPrefix}-deploy-management-groups${parDeploymentSuffix}', 64)
  params: {
    parTelemetryOptOut: true
    parTopLevelManagementGroupDisplayName: parTopLevelManagementGroupName
    parTopLevelManagementGroupPrefix: parDeploymentPrefix
    parTopLevelManagementGroupSuffix: parDeploymentSuffix
    parLandingZoneMgAlzDefaultsEnable: false
    parLandingZoneMgConfidentialEnable: false
    parLandingZoneMgChildren: varLandingZoneMgChildren
    parTopLevelManagementGroupParentId: parTopLevelManagementGroupParentId
    parPlatformMgAlzDefaultsEnable: false
    parPlatformMgChildren: varPlatformMgChildren
  }
}

//This module deploys a management subscription and creates an alias for it.
module modManagementSubscription 'bootstrapDependencies/subscriptionAlias.bicep' = if (empty(parManagementSubscriptionId)) {
  name: take('${parDeploymentPrefix}-deploy-management-subscription${parDeploymentSuffix}', 64)
  params: {
    parSubscriptionBillingScope: parSubscriptionBillingScope
    parSubscriptionName: '${parDeploymentPrefix}-management${parDeploymentSuffix}'
    parManagementGroupId: '${parDeploymentPrefix}-platform-${varPlatformMgChildren.management.displayName}${parDeploymentSuffix}'
    parSubscriptionOfferType: 'Production'
    parTenantId: tenant().tenantId
    parTags: parTags
  }
  dependsOn: [
    modManagementGroups
  ]
}

//This module deploys a connectivity subscription and creates an alias for it
module modConnectivitySubscription 'bootstrapDependencies/subscriptionAlias.bicep' = if (empty(parConnectivitySubscriptionId)) {
  name: take('${parDeploymentPrefix}-deploy-connectivity-subscription${parDeploymentSuffix}', 64)
  params: {
    parSubscriptionBillingScope: parSubscriptionBillingScope
    parSubscriptionName: '${parDeploymentPrefix}-connectivity${parDeploymentSuffix}'
    parManagementGroupId: '${parDeploymentPrefix}-platform-${varPlatformMgChildren.connectivity.displayName}${parDeploymentSuffix}'
    parSubscriptionOfferType: 'Production'
    parTenantId: tenant().tenantId
    parTags: parTags
  }
  dependsOn: [
    modManagementGroups
  ]
}

//This module deploys an identity subscription and creates an alias for it.
module modIdentitySubscription 'bootstrapDependencies/subscriptionAlias.bicep' = if (empty(parIdentitySubscriptionId)) {
  name: take('${parDeploymentPrefix}-deploy-identity-subscription${parDeploymentSuffix}', 64)
  params: {
    parSubscriptionBillingScope: parSubscriptionBillingScope
    parSubscriptionName: '${parDeploymentPrefix}-identity${parDeploymentSuffix}'
    parManagementGroupId: '${parDeploymentPrefix}-platform-${varPlatformMgChildren.identity.displayName}${parDeploymentSuffix}'
    parSubscriptionOfferType: 'Production'
    parTenantId: tenant().tenantId
    parTags: parTags
  }
  dependsOn: [
    modManagementGroups
  ]
}

//This module triggers the create of a landing zone registration 
module modLandingZoneRegistration 'bootstrapDependencies/landingZoneRegistration.bicep' = {
  name: take('${parDeploymentPrefix}-deploy-landing-zone-registration${parDeploymentSuffix}', 64)
  scope: tenant()
   dependsOn: [
    modManagementGroups
  ]
}

output outConnectivitySubscriptionId string = empty(parConnectivitySubscriptionId) ? modConnectivitySubscription.outputs.outSubscriptionId : parConnectivitySubscriptionId
output outManagementSubscriptionId string = empty(parManagementSubscriptionId) ? modManagementSubscription.outputs.outSubscriptionId : parManagementSubscriptionId
output outIdentitySubscriptionId string = empty(parIdentitySubscriptionId) ? modIdentitySubscription.outputs.outSubscriptionId : parIdentitySubscriptionId
output outConnectivitySubscriptionName string = empty(parConnectivitySubscriptionId) ? modConnectivitySubscription.outputs.outSubscriptionName : parConnectivitySubscriptionId
output outManagementSubscriptionName string = empty(parManagementSubscriptionId) ? modManagementSubscription.outputs.outSubscriptionName : parManagementSubscriptionId
output outIdentitySubscriptionName string = empty(parIdentitySubscriptionId) ? modIdentitySubscription.outputs.outSubscriptionName : parIdentitySubscriptionId
output outLandingZoneChildrenManagementGroupIds array = modManagementGroups.outputs.outLandingZoneChildrenManagementGroupIds
output outPlatformChildrenManagementGroupIds array = modManagementGroups.outputs.outPlatformChildrenManagementGroupIds
