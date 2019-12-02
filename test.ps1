$defaultErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference="silentlycontinue"

$testSuccess = $true

function test {
    param (
        $project
    )

	Write-Host "**** Testing $project ****"

	dotnet vstest src\$project\bin\Release\netcoreapp3.0\$project.dll `
		--logger:"trx;LogFileName=$project.trx" `
		--ResultsDirectory:reports `
    
    $script:testSuccess = $script:testSuccess -and ($LastExitCode -eq 0)
}

test "GitHubActionsMSBuildLogger.Tests"

gci -Path .\reports\ -Directory | rm -Recurse

$ErrorActionPreference = $defaultErrorActionPreference

if(!$testSuccess) {
    Write-Host "Test Failed"
    exit 1
}