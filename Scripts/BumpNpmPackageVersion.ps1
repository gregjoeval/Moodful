Param(
[Parameter(Mandatory)]
[string] $packageName,
[string] $bumpKind = "patch"
)

# Reference: https://gist.github.com/kumichou/acefc48476957aad6b0c9abf203c304c
function bumpVersion($kind, $version) {
    $major, $minor, $patch, $build = $version.split('.')

    switch ($kind) {
        "major" {
            $major = [int]$major + 1
            $minor = 0
            $patch = 0
            break;
        }
        "minor" {
            $minor = [int]$minor + 1
            $patch = 0
            break;
        }
        "patch" {
            $patch = [int]$patch + 1
            break;
        }
    }

    return [string]::Format("{0}.{1}.{2}", $major, $minor, $patch)
}

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

Write-Host ("The bumped version of package '{0}' is '{1}'." -f $packageName, $nextVersion)
return $nextVersion