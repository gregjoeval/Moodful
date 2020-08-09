Param(
[Parameter(Mandatory)]
[string] $clientName,
[Parameter(Mandatory)]
[string] $clientVersion,
[string] $outputDirectory = "./client-package",
[string] $specificationFileLocation = "../Swagger/openapi.spec.json"
)

try {
    npm install @openapitools/openapi-generator-cli@latest
} catch {
    Write-Error "`n`n npm install threw but lets keep going...`n`n"
}

try {
    openapi-generator version

    openapi-generator validate -i $specificationFileLocation --recommend

    openapi-generator generate -i $specificationFileLocation -g typescript-fetch -o $('{0}' -f $outputDirectory) -p npmName=$('{0}' -f $clientName) -p npmVersion=$('{0}' -f $clientVersion) -p withInterfaces=true -p useSingleRequestParameter=true -p prefixParameterInterfaces=true 

    Write-Host "`n`n 'openapi-generator' successfully created the client package. `n`n"
} catch {
    Write-Error "`n`n There was an issue while 'openapi-generator' was generating the client package. Check out the logs above. `n`n"
    exit 1
}
