Remove-StoredCredential  -Target 'NINA_CodeSign'
New-StoredCredential -Comment 'NINA Code Sign Password' -Credentials $(Get-Credential) -Target 'NINA_CodeSign'