[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\JfzRDP.csproj'
$solution = Join-Path $root 'JfzRDP.sln'
$outputDirectory = Join-Path $root 'output'
$buildDirectory = Join-Path $root ("bin\{0}" -f $Configuration)

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
if (Test-Path $vswhere) {
    $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null
    if ($found) {
        $msbuild = ($found | Select-Object -First 1).Trim()
    }
}

if (-not $msbuild) {
    $candidates = @(
        'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
        'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
    )
    $msbuild = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $msbuild) {
    throw '未找到 MSBuild.exe，请安装 Visual Studio 的 .NET 桌面开发工作负载。'
}

$axDll = Join-Path $root 'lib\AxInterop.MSTSCLib.dll'
$interopDll = Join-Path $root 'lib\Interop.MSTSCLib.dll'
if (-not (Test-Path $axDll) -or -not (Test-Path $interopDll)) {
    $tlbImp = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\TlbImp.exe'
    $axImp = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\AxImp.exe'
    if (-not (Test-Path $tlbImp)) {
        $tlbImp = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\TlbImp.exe'
    }
    if (-not (Test-Path $axImp)) {
        $axImp = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\AxImp.exe'
    }
    if (-not (Test-Path $tlbImp) -or -not (Test-Path $axImp)) {
        throw '缺少 MSTSCLib 互操作程序集，且未找到 TlbImp.exe / AxImp.exe。'
    }

    New-Item -ItemType Directory -Force (Join-Path $root 'lib') | Out-Null
    Push-Location (Join-Path $root 'lib')
    try {
        & $tlbImp 'C:\Windows\System32\mstscax.dll' '/out:Interop.MSTSCLib.dll' '/namespace:MSTSCLib' /silent
        if ($LASTEXITCODE -ne 0) {
            throw "TlbImp.exe 执行失败，退出码：$LASTEXITCODE"
        }

        & $axImp 'C:\Windows\System32\mstscax.dll' '/rcw:Interop.MSTSCLib.dll' '/out:AxInterop.MSTSCLib.dll' /silent
        if ($LASTEXITCODE -ne 0) {
            throw "AxImp.exe 执行失败，退出码：$LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}

& $msbuild $solution '/t:Rebuild' ("/p:Configuration={0}" -f $Configuration) '/p:Platform=Any CPU' /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    throw "编译失败，MSBuild 退出码：$LASTEXITCODE"
}

New-Item -ItemType Directory -Force $outputDirectory | Out-Null
Copy-Item (Join-Path $buildDirectory 'JfzRDP.exe') $outputDirectory -Force
Copy-Item (Join-Path $root 'lib\AxInterop.MSTSCLib.dll') $outputDirectory -Force
Copy-Item (Join-Path $root 'lib\Interop.MSTSCLib.dll') $outputDirectory -Force

$docPath = Join-Path $root '修改说明.md'
if (Test-Path $docPath) {
    Copy-Item $docPath $outputDirectory -Force
}

Write-Host ("已生成：{0}" -f (Join-Path $outputDirectory 'JfzRDP.exe'))
