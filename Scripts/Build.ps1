﻿param(
    $buildFile   = (join-path (Split-Path -parent $MyInvocation.MyCommand.Definition) "NuGetGallery.msbuild"),
    $buildParams = "/p:Configuration=Release",
    $buildTarget = "/t:CIBuild",
    [switch]$TeamCity
)

if($TeamCity) {
  $ErrorActionPreference = "Stop"
}

& "$(get-content env:windir)\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" $buildFile $buildParams $buildTarget
if($LASTEXITCODE -ne 0) {
    if($TeamCity) {
        Write-Host "##teamcity[message text='Build Failed' status='ERROR']"
        Write-Host "##teamcity[buildStatus status='FAILURE']"
    }
    throw "Build Failed";
    exit 1;
}