#This will add the AXLFormatMessage attribute to the API requests in the generated file.
#This will need to be run again if the file is regenerated from the wsdl
Get-Content "$PSScriptRoot\AXLAPIService.cs" | ForEach-Object {
    if ($_.Contains("[System.ServiceModel.ServiceKnownTypeAttribute(typeof(APIRequest))]"))
    {
        Write-Output "$_`n    [AXLFormatMessage]"
    }
    else
    {
        Write-Output $_
    }
} | Out-File "$PSScriptRoot\output.cs" -Encoding utf8