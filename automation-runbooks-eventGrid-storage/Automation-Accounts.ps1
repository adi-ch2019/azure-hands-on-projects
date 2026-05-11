$varNameStorageAccount='StorageAccount'
$varValueStorageAccount=''
$varNameTenantID='TenantID'
$varValueTenantID=''
$varNameVnet='Vnet'
$varValueVnet=''
$varNameNSG='nsg'
$varValueNSG=''

# create the Automation Account using the New-AzAutomationAccount cmdlet.
#  Here is an example command to create the account:
$resourceGroupName = "YourResourceGroupName"
$automationAccountName = "YourAutomationAccountName"
$location = "YourLocation"

New-AzAutomationAccount -ResourceGroupName $resourceGroupName -Name $automationAccountName -Location $location

# After creating the Automation Account, you can add variables to it using the
#  Set-AzAutomationVariable cmdlet
$variableName = "YourVariableName"
$variableValue = "YourVariableValue"
$automationAccount = Get-AzAutomationAccount -ResourceGroupName $resourceGroupName -Name $automationAccountName

Set-AzAutomationVariable -AutomationAccount $automationAccount -Name $variableName -Value $variableValue

# You can also use the same cmdlet to import a module from the PowerShell Gallery directly.
# Make sure to grab ModuleName and ModuleVersion from the PowerShell Gallery.
# Az.Accouts
# Az.Automation
# Az.Compute
# Az.Network
# Az.Resource
# Az.Storage
# ImportExcel

$moduleName = <ModuleName>
$moduleVersion = <ModuleVersion>
New-AzAutomationModule -AutomationAccountName <AutomationAccountName> \
-ResourceGroupName <ResourceGroupName> -Name $moduleName \
-ContentLinkUri "https://www.powershellgallery.com/api/v2/package/$moduleName/$moduleVersion"
