Param(
[string] $packageName,
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

.\GenerateClientPackage.ps1 -workingDirectory $packageFolder -clientName $packageName -clientVersion $nextVersion

.\PrepareClientPackage.ps1 -workingDirectory $packageFolder

Write-Host ("`n`n The client package was successfully created in folder '{0}'. `n`n " -f $packageFolder)
