Param(
    [string]$blenderPath,
    [string]$heiretsuCli,
    [string]$importScript,
    [string]$dataDir,
    [string]$sgeDir,
    [string]$format,
    [string]$modelName
)

& "$heiretsuCli" export-sge-json -g "$dataDir/grp.bin" -d "$dataDir/dat.bin" -n $modelName -o "$sgeDir/$modelName"
& "$blenderPath" --background -noaudio -P "$importScript" "$sgeDir/$modelName.sge.json" $format