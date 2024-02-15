// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
/*
SUMMARY: The landing Zone Registration(LZR) module deploys a LZR resource in a tenant.
DESCRIPTION:  The module creates a LZR resource which connects a deployed Landing Zone with the Landing Zone Configuration. 
    The Landing Zone is identified by the top level management group id and the Landing Zone Configuration is identified by the id of the landing zone configuration resource.
    The LZR resource is used to link the deployed Landing Zone in Azure and the Landing Zone Configuration.
AUTHOR/S: Microsoft Cloud for Sovereignty Team.
VERSION: 0.1.0
*/

targetScope = 'tenant'

@description('The fully qualified top level Management Group Id of the deployed Landing Zone.')
param parExistingTopLevelMgId string = '/providers/Microsoft.Management/managementGroups/mcfstest'

@description('The fully qualified Id of the associated Landing Zone configuration resource.')
param parExistingLandingZoneConfigurationId string = '/providers/Microsoft.Sovereign/landingzoneconfigurations/testsidlzc57'

var varTopLevelMgName = last(split(parExistingTopLevelMgId, '/'))
var varTopLandingZoneConfigurationName = last(split(parExistingLandingZoneConfigurationId, '/'))

// This is a private preview release of the Microsoft.Sovereign/landingZoneRegistrations resource provider.
// Until the API is not released pubicly, the bicep linter will show the below warning. Suppressing the warning in the interim.
// Warning message - "Resource type "Microsoft.Sovereign/landingZoneRegistrations@2023-09-28-preview" does not have types available.bicep(BCP081)."
#disable-next-line BCP081
resource resLZR 'Microsoft.Sovereign/landingZoneRegistrations@2023-09-28-preview' = {
  name: take('${varTopLevelMgName}-${varTopLandingZoneConfigurationName}', 24)
  properties: {
    existingTopLevelMgId: parExistingTopLevelMgId
    existingLandingZoneConfigurationId: parExistingLandingZoneConfigurationId
  }
}
