# Connect to azure
Connect-AzAccount

# Create a resource group 
$rg = @{
    Name = 'test-rg'
    Location = 'eastus2'
}
New-AzResourceGroup @rg

# Create a virtual network 
$vnet = @{
    Name = 'vnet-1'
    ResourceGroupName = 'test-rg'
    Location = 'eastus2'
    AddressPrefix = '10.0.0.0/16'
}
$virtualNetwork = New-AzVirtualNetwork @vnet

# Deploy subnet within the virtual network 
$subnet = @{
    Name = 'subnet-1'
    VirtualNetwork = $virtualNetwork
    AddressPrefix = "10.0.0.0/24"
}
$subnetConfig = Add-AzVirtualNetworkSubnetConfig @subnet

# Associate the subnet configuration 
$virtualNetwork | Set-AzVirtualNetwork


$rule1 = New-AzNetworkSecurityRuleConfig -Name rdp-rule -Description "Allow RDP" `
    -Access Allow -Protocol Tcp -Direction Inbound -Priority 100 -SourceAddressPrefix `
    Internet -SourcePortRange * -DestinationAddressPrefix * -DestinationPortRange 3389

# $rule2 = New-AzNetworkSecurityRuleConfig -Name web-rule -Description "Allow HTTP" `
#     -Access Allow -Protocol Tcp -Direction Inbound -Priority 101 -SourceAddressPrefix `
#     Internet -SourcePortRange * -DestinationAddressPrefix * -DestinationPortRange 80

$nsg = New-AzNetworkSecurityGroup -ResourceGroupName 'learn-68a0cd7e-bd70-4e20-a36d-a867e72b55d0' -Location eastus2 -Name `
    "nsg-1" -SecurityRules $rule1
    # ,$rule2


# When acting on a storage account, reference the context instead of
# repeatedly passing in the credentials. Use the following example to
# create a storage account with locally redundant 
# storage (LRS) and blob encryption (enabled by default)
$StorageHT = @{
    ResourceGroupName = $rg
    Name = '<storage-account-name>'
    SkuName = 'Standard_LRS'
    Location = $Location
    }
    $StorageAccount = New-AzStorageAccount @StorageHT
    $Context = $StorageAccount.Context

# Install the prerelease version of 
# the Az.ManagedServiceIdentity module to perform the user-assigned
#  managed identity operations in this article.
Install-Module -Name Az.ManagedServiceIdentity -AllowPrerelease

# To create a user-assigned managed identity, your account needs the 
# Managed Identity Contributor role assignment.

# To create a user-assigned managed identity, use the 
# New-AzUserAssignedIdentity command. The ResourceGroupName parameter 
# specifies the resource group where to create the user-assigned managed identity.
#  The -Name parameter specifies its name. Replace the <RESOURCE GROUP> 
#  and <USER ASSIGNED IDENTITY NAME> parameter values with your own values.
New-AzUserAssignedIdentity -ResourceGroupName <RESOURCEGROUP> -Name <USER ASSIGNED IDENTITY NAME>

# To list or read a user-assigned managed identity, your 
# account needs the Managed Identity Operator or Managed Identity Contributor role assignment.
Get-AzUserAssignedIdentity -ResourceGroupName <RESOURCE GROUP>


# To add an assignment of a managed identity to a subscription in Azure using PowerShell, 
# you can use the New-AzRoleAssignment cmdlet. Here's an example of how to do it:
# Replace the placeholders with your own values
$managedIdentityObjectId = "<managed-identity-object-id>"
$subscriptionId = "<subscription-id>"
$roleDefinitionName = "Contributor"

# Assign the role to the managed identity
New-AzRoleAssignment `
  -ObjectId $managedIdentityObjectId `
  -Scope "/subscriptions/$subscriptionId" `
  -RoleDefinitionName $roleDefinitionName
