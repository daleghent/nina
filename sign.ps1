[CmdletBinding()]
param(
    [Parameter(Mandatory, HelpMessage="Path to file that should be signed")]
    [string]$file

)

#Remove-StoredCredential  -Target 'NINA_CodeSign'
$cred = Get-StoredCredential -Target 'NINA_CodeSign'

if($cred -eq $null) {
    Write-Output 'Stored Credentials not found'
    $pw = Read-Host -AsSecureString
    #New-StoredCredential -Comment 'NINA Code Sign Password' -Credentials $(Get-Credential) -Target 'NINA_CodeSign'
    #$pw = Get-StoredCredential -Target 'NINA_CodeSign'
} else {
    $pw = $cred.Password
}

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($pw)
$UnsecurePassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

$keyfile = (get-item $PSScriptRoot).FullName + '\code_sign.pfx'

$app = 'signtool.exe'
$arg0 = 'sign'
$arg1 = '/f ' + $keyfile
$arg2 = '/p ' + $UnsecurePassword
$arg3 = '/t http://timestamp.digicert.com'
$arg4 = '/fd SHA256 ' + $file

#$errFile = $PSScriptRoot + '\ERROR- ' + [System.IO.Path]::GetRandomFileName()
Start-Process $app -ArgumentList "$arg0 $arg1 $arg2 $arg3 $arg4"
