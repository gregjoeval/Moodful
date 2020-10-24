Param(
[Parameter(Mandatory)]
[string] $clientName,
[Parameter(Mandatory)]
[string] $clientVersion,
[string] $outputDirectory = "./client-package",
[string] $specificationFileLocation = "../Swagger/openapi.spec.json"
)

try {
    npm install -g @openapitools/openapi-generator-cli@latest
} catch {
    Write-Error "`n`n npm install threw but lets keep going...`n`n"
}

try {
    openapi-generator-cli version
} catch {
    Write-Error "`n`n There was an issue getting the version of 'openapi-generator-cli'. `n`n"
    exit 1
}

try {
    openapi-generator-cli validate -i $('{0}' -f $specificationFileLocation) --recommend
} catch {
    Write-Error "`n`n There was an issue while 'openapi-generator-cli' was validating the specification file. `n`n"
    exit 1
}

try {
    openapi-generator-cli generate -i $('{0}' -f $specificationFileLocation) -g typescript-fetch -o $('{0}' -f $outputDirectory) -p npmName=$('{0}' -f $clientName) -p npmVersion=$('{0}' -f $clientVersion) -p withInterfaces=true -p useSingleRequestParameter=true -p prefixParameterInterfaces=true 
} catch {
    Write-Error "`n`n There was an issue while 'openapi-generator-cli' was generating the client package. Check out the logs above. `n`n"
    exit 1
}

Write-Host "`n`n 'openapi-generator-cli' successfully created the client package. `n`n"
