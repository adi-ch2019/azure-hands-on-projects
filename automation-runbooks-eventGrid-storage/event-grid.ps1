# Install the Azure PowerShell module, if you haven't already,
#  by running the following command in a PowerShell session:
Install-Module -Name Az -AllowClobber -Scope CurrentUser

Connect-AzAccount

# Create a custom Event Grid topic
New-AzEventGridTopic -ResourceGroupName <resource-group-name> \
-Location <location> -Name <topic-name>

# Retrieve the endpoint and access key for the Event Grid 
# topic you just created by running the following command
$topic = Get-AzEventGridTopic \
-ResourceGroupName <resource-group-name> \
-Name <topic-name>
$endpoint = $topic.Endpoint
$key = $topic.TopicKey

# Optionally, you can create an Event Grid subscription to 
# subscribe to events for the custom topic. Run the 
# following command to create a subscription:
$subscription = New-AzEventGridSubscription \
 -Endpoint <subscription-endpoint> \
 -ResourceId <topic-resource-id>
