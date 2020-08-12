Param(
[string] $workingDirectory = "./client-package"
)

# move .ts files into src
#$filesToMove = @(("{0}/*.ts" -f $workingDirectory), ("{0}/apis" -f $workingDirectory), ("{0}/models" -f $workingDirectory))
#$srcDirectoryPath = ("{0}/src" -f $workingDirectory)
#Write-Host ("Moving files matching '{0}' into '{1}' folder..." -f ($filesToMove -join ', '), $srcDirectoryPath)
#New-Item -ItemType directory -Path $srcDirectoryPath
#Move-Item -Path $filesToMove -Destination $srcDirectoryPath

#
# global-fetch.d.ts
# the client generation template uses typscript 2 which relies on a global variable GlobalFetch, this definition file tells typescript 3 what that is
#

# create file
$definitionFile = ("{0}/src/global-fetch.d.ts" -f $workingDirectory)
New-Item $definitionFile -Force

# only necessary for typescript 2
Set-Content $definitionFile @'
declare interface GlobalFetch {
    fetch(input: RequestInfo, init?: RequestInit): Promise<Response>;
}
'@

#
# package.json
# the client generation template provides a tsconfig.json but let's make sure these values are what we expect
#

# read from file
$packageJsonFile = ("{0}/package.json" -f $workingDirectory) 
$packageJsonData = Get-Content $packageJsonFile -raw | ConvertFrom-Json

# parse client name from package name
$clientName = $packageJsonData.name -replace '.*@dealerpolicy/'
Write-Host $clientName

# add devDependency
$packageJsonData.devDependencies | Add-Member -Force -NotePropertyName tsdx -NotePropertyValue "latest"
$packageJsonData.devDependencies | Add-Member -Force -NotePropertyName tslib -NotePropertyValue "latest"
$packageJsonData.devDependencies | Add-Member -Force -NotePropertyName @microsoft/api-extractor -NotePropertyValue "beta"

# add tsdx build script
# set name parameter to client name for entry files (https://tsdx.io/api-reference#tsdx-build)
$packageJsonData.scripts | Add-Member -Force -NotePropertyName setup -NotePropertyValue ("npm install")
$packageJsonData.scripts | Add-Member -Force -NotePropertyName build -NotePropertyValue ("npm run build:dist && npm run build:dtsRollup")
$packageJsonData.scripts | Add-Member -Force -NotePropertyName build:dist -NotePropertyValue ("tsdx build --format cjs,esm --name {0}" -f $clientName)
$packageJsonData.scripts | Add-Member -Force -NotePropertyName build:dtsRollup -NotePropertyValue ("api-extractor run")

# remove prepare script
# prepare fires too many times, just run build (https://docs.npmjs.com/misc/scripts)
$packageJsonData.scripts = $packageJsonData.scripts | Select-Object -Property * -ExcludeProperty prepare

# define entry files
$typeDefinitionFile = ("dist/{0}.d.ts" -f $clientName)

# add entry files
$packageJsonData | Add-Member -Force -NotePropertyName main -NotePropertyValue "dist/index.js"
$packageJsonData | Add-Member -Force -NotePropertyName types -NotePropertyValue $typeDefinitionFile
$packageJsonData | Add-Member -Force -NotePropertyName typings -NotePropertyValue $typeDefinitionFile
$packageJsonData | Add-Member -Force -NotePropertyName module -NotePropertyValue ("dist/{0}.esm.js" -f $clientName)

# add sideEffects property (https://webpack.js.org/guides/tree-shaking/)
$packageJsonData | Add-Member -Force -NotePropertyName sideEffects -NotePropertyValue $false

# add repository
$packageJsonData | Add-Member -Force -NotePropertyName repository -NotePropertyValue @{ type="git", url="git+https://github.com/gregjoeval/Moodful.git" }

# add files
$packageJsonData | Add-Member -Force -NotePropertyName files -NotePropertyValue @(
    "dist/*.js",
    "dist/*.js.map",
    $typeDefinitionFile,
    "dist/tsdoc-metadata.json", # included so its easier to see how the package was generated
    "src",
    ".openapi-generator", # included so its easier to see how the package was generated
    "tsconfig.json" # included so its easier to see how the package was generated
)

# write to file
$packageJsonString = ConvertTo-Json -Depth 10 $packageJsonData
Write-Host $packageJsonString
$packageJsonString | Out-File $packageJsonFile -Encoding ASCII -Force

#
# tsconfig.json
# the client generation template provides a tsconfig.json but let's make sure these values are what we expect
#

# read from file
$tsconfigJsonFile = ("{0}/tsconfig.json" -f $workingDirectory)
$tsconfigJsonData = Get-Content $tsconfigJsonFile -raw | ConvertFrom-Json

# edit compilerOptions (https://github.com/formium/tsdx/blob/master/templates/basic/tsconfig.json)
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName rootDir -NotePropertyValue "src"
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName outDir -NotePropertyValue "dist"
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName target -NotePropertyValue "es5"
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName module -NotePropertyValue "esnext"
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName moduleResolution -NotePropertyValue "Node"
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName importHelpers -NotePropertyValue $true
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName esModuleInterop -NotePropertyValue $true
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName allowSyntheticDefaultImports -NotePropertyValue $true
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName declaration -NotePropertyValue $true
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName declarationMap -NotePropertyValue $true
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName sourceMap -NotePropertyValue $true
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName lib -NotePropertyValue @("esnext", "dom")
$tsconfigJsonData.compilerOptions | Add-Member -Force -NotePropertyName typeRoots -NotePropertyValue @("src/global-fetch.d.ts", "node_modules/@types")

# edit include
$tsconfigJsonData | Add-Member -Force -NotePropertyName include -NotePropertyValue @("src")

# write to file
$tsconfigJsonString = ConvertTo-Json -Depth 10 $tsconfigJsonData
Write-Host $tsconfigJsonString
$tsconfigJsonString | Out-File $tsconfigJsonFile -Encoding ASCII -Force

#
# api-extractor.json
#

# use serialized JSON string
# Note: this was easier to work with because $schema had a '$' in the property nam
$apiExtractorJsonString = @('{"$schema":"https://developer.microsoft.com/json-schemas/api-extractor/v7/api-extractor.schema.json","mainEntryPointFilePath":"<projectFolder>/dist/index.d.ts","bundledPackages":[],"compiler":{},"apiReport":{"enabled":false},"docModel":{"enabled":false},"dtsRollup":{"enabled":true},"tsdocMetadata":{},"messages":{"compilerMessageReporting":{"default":{"logLevel":"none"}},"extractorMessageReporting":{"default":{"logLevel":"none"}},"tsdocMessageReporting":{"default":{"logLevel":"none"}}}}')

# write to file
$apiExtractorJsonFile = ("{0}/api-extractor.json" -f $workingDirectory)
Write-Host $apiExtractorJsonString
$apiExtractorJsonString | Set-Content $apiExtractorJsonFile -Encoding ASCII -Force