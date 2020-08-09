Param(
[Parameter(Mandatory)]
[string] $clientName,
[Parameter(Mandatory)]
[string] $clientVersion,
[string] $workingDirectory = "./client-package"
)

try {
    npm install @openapitools/openapi-generator-cli@latest
} catch {
    Write-Error "`n`n npm install threw but lets keep going...`n`n"
}

try {
    openapi-generator version

    openapi-generator validate -i openapi.spec.json --recommend

    openapi-generator generate -i openapi.spec.json -g typescript-fetch -o $('{0}' -f $workingDirectory) -p npmName=$('{0}' -f $clientName) -p npmVersion=$('{0}' -f $clientVersion) -p withInterfaces=true -p useSingleRequestParameter=true -p prefixParameterInterfaces=true 

    Write-Host "`n`n 'openapi-generator' successfully created the client package. `n`n"
} catch {
    Write-Error "`n`n There was an issue while 'openapi-generator' was generating the client package. Check out the logs above. `n`n"
    exit 1
}
