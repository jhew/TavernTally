signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 `
  /f path\to\cert.pfx /p $env:SIGN_PWD `
  "src\Overlay.App\bin\Release\net8.0-windows\Overlay.App.exe"

signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 `
  /f path\to\cert.pfx /p $env:SIGN_PWD `
  "installer\bin\Release\en-us\TavernTally.msi"