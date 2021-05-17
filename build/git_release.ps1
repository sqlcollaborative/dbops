$moduleFile = ".\dbops.psd1"
$moduleData = Invoke-Command -ScriptBlock ([scriptblock]::Create((Get-Content $moduleFile -Raw)))
$version = [Version]$moduleData.ModuleVersion
git config --global user.email $env:git_user_email
git config --global user.name $env:git_username
git add $moduleFile
git commit -m "v$version"
git push origin HEAD:development
git branch release/$version
git push --all origin