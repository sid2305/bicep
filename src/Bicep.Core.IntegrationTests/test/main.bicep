// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
/*
SUMMARY: The main module is the entry point for deploying the entire Sovereign Landing Zone (SLZ).
DESCRIPTION:  The module deploys the entire SLZ. Please refer to the details in the README file on how to invoke it using Azure Powershell/Azure CLI
AUTHOR/S: Microsoft Cloud for Sovereignty Team.
VERSION: 0.1.0
*/

targetScope = 'tenant'

@description('The prefix that will be added to all resources created by this deployment.')
@minLength(2)
@maxLength(5)
param parDeploymentPrefix string = 'mcfs'

@description('The suffix that will be added to management group suffix name the same way to be added to management group prefix names.')
@maxLength(5)
param parDeploymentSuffix string = 'test'

module modBootstrap 'bootstrap.bicep' = {
  name: '${parDeploymentPrefix}-bootstrap${parDeploymentSuffix}-deployment'
}

module modPlatform 'platform.bicep' = {
  name: '${parDeploymentPrefix}-platform${parDeploymentSuffix}-deployment'
  scope: managementGroup('${parDeploymentPrefix}${parDeploymentSuffix}')
  params: {
    parConnectivitySubscriptionId: modBootstrap.outputs.outConnectivitySubscriptionId
    parIdentitySubscriptionId: modBootstrap.outputs.outIdentitySubscriptionId
    parManagementSubscriptionId: modBootstrap.outputs.outManagementSubscriptionId
  }
  dependsOn: [
    modBootstrap
  ]
}

module modCompliance 'compliance.bicep' = {
  name: '${parDeploymentPrefix}-compliance${parDeploymentSuffix}-deployment'
  scope: managementGroup('${parDeploymentPrefix}${parDeploymentSuffix}')
  dependsOn: [
    modBootstrap
  ]
}

module dashboardModule 'dashboard.bicep' = {
  name: '${parDeploymentPrefix}-dashboard${parDeploymentSuffix}-deployment'
  scope: managementGroup('${parDeploymentPrefix}${parDeploymentSuffix}')
  params: {
    parManagementSubscriptionId: modBootstrap.outputs.outManagementSubscriptionId
  }
  dependsOn: [
    modCompliance
  ]
}