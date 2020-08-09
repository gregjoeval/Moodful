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
    openapi-generator version
} catch {
    Write-Error "`n`n There was an issue getting the version of 'openapi-generator'. `n`n"
    exit 1
}

try {
    openapi-generator validate -i $('{0}' -f $specificationFileLocation) --recommend
} catch {
    Write-Error "`n`n There was an issue while 'openapi-generator' was validating the specification file. `n`n"
    exit 1
}

try {
    openapi-generator generate -i $('{0}' -f $specificationFileLocation) -g typescript-fetch -o $('{0}' -f $outputDirectory) -p npmName=$('{0}' -f $clientName) -p npmVersion=$('{0}' -f $clientVersion) -p withInterfaces=true -p useSingleRequestParameter=true -p prefixParameterInterfaces=true 
} catch {
    Write-Error "`n`n There was an issue while 'openapi-generator' was generating the client package. Check out the logs above. `n`n"
    exit 1
}

Write-Host "`n`n 'openapi-generator' successfully created the client package. `n`n"
