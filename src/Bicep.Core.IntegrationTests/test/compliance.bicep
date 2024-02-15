/*
  SUMMARY : Deploys the PolicyInitiatives for the Sovereign Landing Zone
  AUTHOR/S: Cloud for Sovereignty
*/
targetScope = 'managementGroup'

@description('Deployment location')
@allowed([
  'australiacentral'
  'australiacentral2'
  'australiaeast'
  'australiasoutheast'
  'brazilsouth'
  'brazilsoutheast'
  'brazilus'
  'canadacentral'
  'canadaeast'
  'centralindia'
  'centralus'
  'centraluseuap'
  'eastasia'
  'eastus'
  'eastus2'
  'eastus2euap'
  'eastusstg'
  'francecentral'
  'francesouth'
  'germanynorth'
  'germanywestcentral'
  'israelcentral'
  'italynorth'
  'japaneast'
  'japanwest'
  'jioindiacentral'
  'jioindiawest'
  'koreacentral'
  'koreasouth'
  'northcentralus'
  'northeurope'
  'norwayeast'
  'norwaywest'
  'polandcentral'
  'qatarcentral'
  'southafricanorth'
  'southafricawest'
  'southcentralus'
  'southcentralusstg'
  'southeastasia'
  'southindia'
  'swedencentral'
  'switzerlandnorth'
  'switzerlandwest'
  'uaecentral'
  'uaenorth'
  'uksouth'
  'ukwest'
  'westcentralus'
  'westeurope'
  'westindia'
  'westus'
  'westus2'
  'westus3'
])
#disable-next-line no-unused-params
param parDeploymentLocation string = 'eastus'






module modmcfsplatformconnectiviAllowUsageCostResources 'complianceDependencies/AllowUsageCostResources-PolAssign-1d9d60f1fcb2e3aff00d0b59a.bicep' = {
	name: 'mcfs-AllowUsageCostResou'
	scope: managementGroup('mcfs-platform-connectivitytest')
	params:{
		deploymentLocation: parDeploymentLocation
	}
}






