Param(
[string] $packageName = "@gjv/moodful-api-client",
[string] $packageFolder = "./client-package",
[string] $packageVersion,
[string] $bumpKind = "patch"
)

# Reference: https://gist.github.com/kumichou/acefc48476957aad6b0c9abf203c304c
function bumpVersion($kind, $version) {
    $major, $minor, $patch, $build = $version.split('.')

    switch ($kind) {
        "major" {
            $major = [int]$major + 1
        }
        "minor" {
            $minor = [int]$minor + 1
        }
        "patch" {
            $patch = [int]$patch + 1
        }
    }

    return [string]::Format("{0}.{1}.{2}", $major, $minor, $patch)
}

# use version provided if any
if ($packageVersion.Length -gt 0) {
    $nextVersion = $packageVersion
} else {
    # otherwise, bump the version of the package in the registry
    try {
        $currentVersion = npm view $packageName version
        Write-Host ("The current version of package '{0}' is '{1}'." -f $packageName, $currentVersion)
    } catch {
        Write-Error ("A version must be specified if the package '{0}' does not exist in the current registry." -f $packageName)
    }

    $validBumpKinds = @("major", "minor", "patch")

    if (-not $validBumpKinds.Contains($bumpKind))
    {
        Write-Output ("Invalid bumpKind '{0}'!" -f $bumpKind)
        exit 1
    }

    $nextVersion = bumpVersion $bumpKind $currentVersion
}

Write-Host ("The next version of package '{0}' will be '{1}'. `n`n " -f $packageName, $nextVersion)

.\GenerateClientPackage.ps1 -workingDirectory $packageFolder -clientName $packageName -clientVersion $nextVersion

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
