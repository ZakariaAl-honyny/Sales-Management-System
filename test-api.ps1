$baseUrl = "http://localhost:5221"

Write-Host "=== API Tests ===" -ForegroundColor Cyan

# 1. Health
$health = Invoke-RestMethod -Uri "$baseUrl/api/v1/health" -Method Get
Write-Host "1. Health: OK - $($health.status)"

# 2. Login
$login = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" -Method Post -Body '{"userName":"admin","password":"admin123"}' -ContentType "application/json"
$token = $login.token
Write-Host "2. Login: OK - Token received"

$headers = @{ "Authorization" = "Bearer $token" }

# 3-13: Test all endpoints
$endpoints = @(
    "categories",
    "products",
    "customers",
    "suppliers",
    "warehouses",
    "units",
    "purchase-invoices",
    "sales-invoices",
    "sales-returns",
    "purchase-returns",
    "stock-transfers",
    "payments/customer",
    "payments/supplier"
)

$i = 3
foreach ($ep in $endpoints) {
    try {
        $res = Invoke-RestMethod -Uri "$baseUrl/api/v1/$ep" -Method Get -Headers $headers -ErrorAction SilentlyContinue
        if ($res) { Write-Host "$i. /$ep : 200 OK" }
        else { Write-Host "$i. /$ep : ERROR" }
    } catch {
        Write-Host "$i. /$ep : FAILED - $($_.Exception.Message.Substring(0,50))"
    }
    $i++
}

Write-Host "`n=== All Tests Complete ===" -ForegroundColor Green