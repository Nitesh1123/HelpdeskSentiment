# Automated API Test Suite for Helpdesk Sentiment Web API
# Tests Calls API and Agents API with HTTP Basic Authentication

param(
    [string]$BaseUrl = "http://localhost:5153"
)

function Send-ApiRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [string]$Username = $null,
        [string]$Password = $null,
        [string]$Body = $null,
        [string]$ContentType = "application/json"
    )
    $headers = @{}
    if ($Username -and $Password) {
        $pair = "$($Username):$($Password)"
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($pair)
        $base64 = [Convert]::ToBase64String($bytes)
        $headers["Authorization"] = "Basic $base64"
    }

    try {
        $params = @{
            Method = $Method
            Uri = $Uri
            Headers = $headers
            UseBasicParsing = $true
        }
        if ($Body) {
            $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($Body)
            $params["ContentType"] = $ContentType
        }
        $resp = Invoke-WebRequest @params
        return [PSCustomObject]@{
            StatusCode = [int]$resp.StatusCode
            Content = $resp.Content
        }
    }
    catch [System.Net.WebException] {
        $webResp = $_.Exception.Response
        if ($webResp) {
            $reader = New-Object System.IO.StreamReader($webResp.GetResponseStream())
            $errContent = $reader.ReadToEnd()
            return [PSCustomObject]@{
                StatusCode = [int]$webResp.StatusCode
                Content = $errContent
            }
        }
        throw $_
    }
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Assert-Endpoint {
    param(
        [string]$TestName,
        [string]$Method,
        [string]$Path,
        [int]$ExpectedStatus,
        [string]$Username = $null,
        [string]$Password = $null,
        [string]$Body = $null,
        [scriptblock]$ExtraCheck = $null
    )

    $url = "$BaseUrl$Path"
    $resp = Send-ApiRequest -Method $Method -Uri $url -Username $Username -Password $Password -Body $Body
    $passed = ($resp.StatusCode -eq $ExpectedStatus)

    if ($passed -and $ExtraCheck) {
        try {
            $extraResult = & $ExtraCheck $resp
            if ($extraResult -eq $false) {
                $passed = $false
            }
        }
        catch {
            $passed = $false
        }
    }

    $resultObj = [PSCustomObject]@{
        TestName = $TestName
        Method = $Method
        Endpoint = $Path
        Expected = $ExpectedStatus
        Actual = $resp.StatusCode
        Result = if ($passed) { "PASS" } else { "FAIL" }
    }
    $results.Add($resultObj)
    return $resp
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " Running Helpdesk Sentiment Web API Test Suite against $BaseUrl" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

# 1. No credentials on protected endpoint -> 401 Unauthorized
$null = Assert-Endpoint -TestName "Calls - Unauthenticated" -Method "GET" -Path "/api/calls" -ExpectedStatus 401

# 2. Get all calls as admin -> 200 OK
$null = Assert-Endpoint -TestName "Calls - Get All (Admin)" -Method "GET" -Path "/api/calls" -ExpectedStatus 200 -Username "admin" -Password "Admin@123"

# 3. Create call -> 201 Created
$newCallPayload = @"
{
  "callerName": "Automation Test Caller",
  "phoneNumber": "9811223344",
  "category": "Technical",
  "agentId": 1,
  "transcript": "Internet is finally working perfectly and fast. Great support and very polite agent!",
  "durationSeconds": 180
}
"@
$postResp = Assert-Endpoint -TestName "Calls - Create Call (Admin)" -Method "POST" -Path "/api/calls" -ExpectedStatus 201 -Username "admin" -Password "Admin@123" -Body $newCallPayload

$createdId = 1
try {
    $createdJson = $postResp.Content | ConvertFrom-Json
    if ($createdJson.id) {
        $createdId = $createdJson.id
    }
}
catch {}

# 4. Get call by id -> 200 OK
$null = Assert-Endpoint -TestName "Calls - Get By ID" -Method "GET" -Path "/api/calls/$createdId" -ExpectedStatus 200 -Username "admin" -Password "Admin@123"

# 5. Update call -> 204 No Content
$updatePayload = @"
{
  "callerName": "Automation Test Caller Updated",
  "phoneNumber": "9811223344",
  "category": "Technical",
  "agentId": 1,
  "transcript": "Issue returned and router crashed again, very frustrating and terrible service.",
  "durationSeconds": 240
}
"@
$null = Assert-Endpoint -TestName "Calls - Update Call" -Method "PUT" -Path "/api/calls/$createdId" -ExpectedStatus 204 -Username "admin" -Password "Admin@123" -Body $updatePayload

# 6. Invalid body -> 400 Bad Request
$invalidPayload = @"
{
  "callerName": "X",
  "phoneNumber": "123"
}
"@
$null = Assert-Endpoint -TestName "Calls - Invalid Body" -Method "POST" -Path "/api/calls" -ExpectedStatus 400 -Username "admin" -Password "Admin@123" -Body $invalidPayload

# 7. Missing id -> 404 Not Found
$null = Assert-Endpoint -TestName "Calls - Missing ID" -Method "GET" -Path "/api/calls/999999" -ExpectedStatus 404 -Username "admin" -Password "Admin@123"

# 8. Agent on admin-only endpoint (DELETE call) -> 403 Forbidden
$null = Assert-Endpoint -TestName "Calls - Delete as Agent (Forbidden)" -Method "DELETE" -Path "/api/calls/$createdId" -ExpectedStatus 403 -Username "agent" -Password "Agent@123"

# 9. Admin delete call -> 204 No Content
$null = Assert-Endpoint -TestName "Calls - Delete as Admin" -Method "DELETE" -Path "/api/calls/$createdId" -ExpectedStatus 204 -Username "admin" -Password "Admin@123"

# 10. Agents - Unauthenticated -> 401 Unauthorized
$null = Assert-Endpoint -TestName "Agents - Unauthenticated" -Method "GET" -Path "/api/agents" -ExpectedStatus 401

# 11. Agents - Get as Agent (Forbidden) -> 403 Forbidden
$null = Assert-Endpoint -TestName "Agents - Get as Agent (Forbidden)" -Method "GET" -Path "/api/agents" -ExpectedStatus 403 -Username "agent" -Password "Agent@123"

# 12. Agents - Get as Admin -> 200 OK
$null = Assert-Endpoint -TestName "Agents - Get as Admin" -Method "GET" -Path "/api/agents" -ExpectedStatus 200 -Username "admin" -Password "Admin@123"

Write-Host ""
$table = $results | Format-Table -AutoSize | Out-String
Write-Host $table

$totalCount = $results.Count
$passCount = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$failCount = ($results | Where-Object { $_.Result -eq "FAIL" }).Count

$color = if ($failCount -eq 0) { "Green" } else { "Red" }
Write-Host $summary -ForegroundColor $color

# Save output to docs/screenshots/api-test-results.txt
$outputContent = @"
================================================================
           Helpdesk Sentiment Web API Test Suite Results
================================================================
Executed at: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Target Base URL: $BaseUrl

$table
$summary
"@

$outputDir = Split-Path -Parent "docs\screenshots\api-test-results.txt"
if (!(Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir -Force }
[System.IO.File]::WriteAllText((Join-Path (Get-Location) "docs\screenshots\api-test-results.txt"), $outputContent)
Write-Host "Results saved to docs/screenshots/api-test-results.txt"
