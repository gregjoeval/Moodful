Param(
[string] $packageName = "@gjv/moodful-api-client",
[string] $packageFolder = "./client-package",
[string] $packageVersionOverride,
[string] $bumpKind = "patch"
)

# use version provided if any
if ($packageVersionOverride.Length -gt 0) {
    $nextVersion = $packageVersionOverride
} else {
    # otherwise, bump the version of the package in the registry
    $nextVersion = .\BumpNpmPackageVersion.ps1 -packageName $packageName -bumpKind $bumpKind
}

.\GenerateClientPackage.ps1 -outputDirectory $packageFolder -clientName $packageName -clientVersion $nextVersion

.\PrepareClientPackage.ps1 -workingDirectory $packageFolder

Write-Host ("`n`n The client package was successfully created in folder '{0}'. `n`n " -f $packageFolder)

# change location to client package folder
Push-Location $packageFolder

try {
    npm install
} catch {
    Write-Error "`n`n npm install threw but lets keep going...`n`n"
}

npm run build

npm publish --access public

# change location back to original folder
Pop-Location

Write-Host "`n`n Done. `n`n"
