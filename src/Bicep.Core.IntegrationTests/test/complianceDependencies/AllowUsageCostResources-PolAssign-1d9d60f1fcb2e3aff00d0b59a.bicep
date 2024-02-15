targetScope = 'managementGroup'

param deploymentLocation string = deployment().location
param parPolicyAssignmentDefinitionId string = '/providers/Microsoft.Authorization/policySetDefinitions/0a2ebd47-3fb9-4735-a006-b7f31ddadd9f'

resource resAllowUsageCostResources 'Microsoft.Authorization/policyAssignments@2022-06-01' = {
  name: '4c9bf528e22b5d6bcda49e8c'
  location: deploymentLocation
  properties: {
    displayName: 'Allow Usage Cost Resources'
    policyDefinitionId: parPolicyAssignmentDefinitionId
    parameters: {
 
    }
  }
  identity: {
		type: 'SystemAssigned'
	}
  }