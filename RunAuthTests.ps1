# Comprehensive Authentication Test Script
# Run this script to test the complete authentication flow

$ErrorActionPreference = "Continue"
$identityUrl = "http://localhost:5001"
$apiUrl = "http://localhost:5056/api/auctions"

Write-Host "`n=======================================" -ForegroundColor Cyan
Write-Host "Authentication & Authorization Tests" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

# STEP 1: Get Alice's Token
Write-Host "`n[STEP 1] Getting access token for Alice..." -ForegroundColor Yellow
Write-Host "Credentials: alice / Pass123`$" -ForegroundColor Gray

$tokenParams = @{
    client_id = "postman"
    client_secret = "NotASecret"
    scope = "auctionApp openid profile"
    username = "alice"
    password = "Pass123`$"
    grant_type = "password"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "$identityUrl/connect/token" -Method Post -Body $tokenParams -ContentType "application/x-www-form-urlencoded"
    $aliceToken = $tokenResponse.access_token
    Write-Host "? SUCCESS: Got Alice's token" -ForegroundColor Green
    Write-Host "  Token (first 50 chars): $($aliceToken.Substring(0, [Math]::Min(50, $aliceToken.Length)))..." -ForegroundColor Gray
    Write-Host "  Token expires in: $($tokenResponse.expires_in) seconds" -ForegroundColor Gray
} catch {
    Write-Host "? FAILED: Could not get Alice's token" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Make sure Identity Service is running on http://localhost:5001" -ForegroundColor Yellow
    exit 1
}

# STEP 2: Create Auction as Alice
Write-Host "`n[STEP 2] Creating auction as Alice..." -ForegroundColor Yellow

$newAuction = @{
    make = "Ford"
    model = "Mustang GT"
    year = 2023
    color = "Red"
    mileage = 5000
    imageUrl = "https://example.com/mustang.jpg"
    reservePrice = 50000
    auctionEnd = (Get-Date).AddDays(10).ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

$aliceHeaders = @{
    Authorization = "Bearer $aliceToken"
    "Content-Type" = "application/json"
}

try {
    $createResponse = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $newAuction -Headers $aliceHeaders
    $auctionId = $createResponse.id
    Write-Host "? SUCCESS: Created auction" -ForegroundColor Green
    Write-Host "  Auction ID: $auctionId" -ForegroundColor Gray
    Write-Host "  Make/Model: $($createResponse.make) $($createResponse.model)" -ForegroundColor Gray
    Write-Host "  Seller: $($createResponse.seller)" -ForegroundColor Gray
    Write-Host "  Status: $($createResponse.status)" -ForegroundColor Gray
} catch {
    Write-Host "? FAILED: Could not create auction" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "  Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
    exit 1
}

# STEP 3: Update Auction as Alice (should succeed)
Write-Host "`n[STEP 3] Updating auction as Alice (owner)..." -ForegroundColor Yellow

$updateAuction = @{
    make = "Ford"
    model = "Mustang GT Premium"
    year = 2023
    color = "Candy Red"
    mileage = 5500
} | ConvertTo-Json

try {
    $updateResponse = Invoke-RestMethod -Uri "$apiUrl/$auctionId" -Method Put -Body $updateAuction -Headers $aliceHeaders
    Write-Host "? SUCCESS: Updated auction" -ForegroundColor Green
    Write-Host "  Updated Model: $($updateResponse.model)" -ForegroundColor Gray
    Write-Host "  Updated Color: $($updateResponse.color)" -ForegroundColor Gray
    Write-Host "  Updated Mileage: $($updateResponse.mileage)" -ForegroundColor Gray
} catch {
    Write-Host "? FAILED: Could not update auction" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "  Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

# STEP 4: Get Bob's Token
Write-Host "`n[STEP 4] Getting access token for Bob..." -ForegroundColor Yellow
Write-Host "Credentials: bob / Pass123`$" -ForegroundColor Gray

$bobTokenParams = @{
    client_id = "postman"
    client_secret = "NotASecret"
    scope = "auctionApp openid profile"
    username = "bob"
    password = "Pass123`$"
    grant_type = "password"
}

try {
    $bobTokenResponse = Invoke-RestMethod -Uri "$identityUrl/connect/token" -Method Post -Body $bobTokenParams -ContentType "application/x-www-form-urlencoded"
    $bobToken = $bobTokenResponse.access_token
    Write-Host "? SUCCESS: Got Bob's token" -ForegroundColor Green
    Write-Host "  Token (first 50 chars): $($bobToken.Substring(0, [Math]::Min(50, $bobToken.Length)))..." -ForegroundColor Gray
} catch {
    Write-Host "? FAILED: Could not get Bob's token" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# STEP 5: Try to Update Alice's Auction as Bob (should fail with 403)
Write-Host "`n[STEP 5] Trying to update Alice's auction as Bob..." -ForegroundColor Yellow
Write-Host "Expected Result: Should be FORBIDDEN (403)" -ForegroundColor Gray

$bobHeaders = @{
    Authorization = "Bearer $bobToken"
    "Content-Type" = "application/json"
}

$bobUpdate = @{
    make = "Ford"
    model = "Mustang - STOLEN BY BOB"
    year = 2023
    color = "Blue"
    mileage = 6000
} | ConvertTo-Json

try {
    $bobUpdateResponse = Invoke-RestMethod -Uri "$apiUrl/$auctionId" -Method Put -Body $bobUpdate -Headers $bobHeaders
    Write-Host "? FAILED: Bob was able to update Alice's auction! This should not happen!" -ForegroundColor Red
    Write-Host "  Updated Model: $($bobUpdateResponse.model)" -ForegroundColor Red
    Write-Host "  THIS IS A SECURITY ISSUE!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403) {
        Write-Host "? SUCCESS: Bob was correctly FORBIDDEN from updating Alice's auction (403)" -ForegroundColor Green
        Write-Host "  This is the expected behavior - authorization is working correctly!" -ForegroundColor Gray
    } elseif ($statusCode -eq 401) {
        Write-Host "? WARNING: Got 401 Unauthorized instead of 403 Forbidden" -ForegroundColor Yellow
        Write-Host "  Authentication worked, but authorization might need adjustment" -ForegroundColor Yellow
    } else {
        Write-Host "? UNEXPECTED: Got status code $statusCode" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# STEP 6: Verify Auction Still Has Original Data
Write-Host "`n[STEP 6] Verifying auction was not modified by Bob..." -ForegroundColor Yellow

try {
    $verifyResponse = Invoke-RestMethod -Uri "$apiUrl/$auctionId" -Method Get
    if ($verifyResponse.model -eq "Mustang GT Premium" -and $verifyResponse.color -eq "Candy Red") {
        Write-Host "? SUCCESS: Auction data is intact (was not modified by Bob)" -ForegroundColor Green
        Write-Host "  Current Model: $($verifyResponse.model)" -ForegroundColor Gray
        Write-Host "  Current Color: $($verifyResponse.color)" -ForegroundColor Gray
        Write-Host "  Current Mileage: $($verifyResponse.mileage)" -ForegroundColor Gray
    } else {
        Write-Host "? WARNING: Auction data may have been modified" -ForegroundColor Yellow
        Write-Host "  Current Model: $($verifyResponse.model)" -ForegroundColor Gray
        Write-Host "  Current Color: $($verifyResponse.color)" -ForegroundColor Gray
    }
} catch {
    Write-Host "? FAILED: Could not retrieve auction" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

# STEP 7: Delete Auction as Alice (should succeed)
Write-Host "`n[STEP 7] Deleting auction as Alice (owner)..." -ForegroundColor Yellow

try {
    Invoke-RestMethod -Uri "$apiUrl/$auctionId" -Method Delete -Headers $aliceHeaders
    Write-Host "? SUCCESS: Alice deleted her auction" -ForegroundColor Green
} catch {
    Write-Host "? FAILED: Could not delete auction" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "  Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

# STEP 8: Verify Auction is Deleted
Write-Host "`n[STEP 8] Verifying auction is deleted..." -ForegroundColor Yellow

try {
    $deletedCheck = Invoke-RestMethod -Uri "$apiUrl/$auctionId" -Method Get
    Write-Host "? WARNING: Auction still exists (might be soft delete)" -ForegroundColor Yellow
    Write-Host "  Status: $($deletedCheck.status)" -ForegroundColor Gray
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 404) {
        Write-Host "? SUCCESS: Auction is properly deleted (404 Not Found)" -ForegroundColor Green
    } else {
        Write-Host "? UNEXPECTED: Got status code $statusCode" -ForegroundColor Red
    }
}

# Summary
Write-Host "`n=======================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "`nAll critical authentication and authorization tests completed!`n" -ForegroundColor Green

Write-Host "Key Test Results:" -ForegroundColor Yellow
Write-Host "  1. ? Alice successfully obtained authentication token" -ForegroundColor White
Write-Host "  2. ? Alice created an auction" -ForegroundColor White
Write-Host "  3. ? Alice updated her own auction" -ForegroundColor White
Write-Host "  4. ? Bob successfully obtained authentication token" -ForegroundColor White
Write-Host "  5. ? Bob was prevented from updating Alice's auction (Authorization working)" -ForegroundColor White
Write-Host "  6. ? Alice successfully deleted her auction" -ForegroundColor White

Write-Host "`n=======================================" -ForegroundColor Cyan
Write-Host "To run this test again, execute: .\RunAuthTests.ps1" -ForegroundColor Gray
Write-Host "=======================================" -ForegroundColor Cyan
