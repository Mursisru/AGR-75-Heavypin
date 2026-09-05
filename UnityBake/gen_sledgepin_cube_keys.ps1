# Bake Sledgepin fin deploy keys from sanitized Unity .anim clips (Crosswim-style).
$ErrorActionPreference = "Stop"
$Fps = 24.0
$FrameCount = 49
$Names = @(
    "Cube-1-001", "Cube-1-002", "Cube-1-003", "Cube-1-004",
    "Cube-2-001", "Cube-2-002", "Cube-2-003", "Cube-2-004"
)
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ClipDir = Join-Path $Root "Assets\MissilePack\AnimClips"
$OutCs = Join-Path (Split-Path -Parent $Root) "AGR75Sledgepin\Runtime\SledgepinCubeKeys.cs"

function Resolve-Clip([string]$FinName) {
    $safe = $FinName -replace "-", "_"
    $hits = Get-ChildItem -Path $ClipDir -Filter "${safe}_*.anim" | Sort-Object Name
    if ($hits.Count -eq 0) { throw "No anim clip for $FinName in $ClipDir" }
    return $hits[0].FullName
}

function Parse-FloatCurves([string]$Text) {
    $curves = @{}
    $start = $Text.IndexOf("m_EditorCurves:")
    if ($start -lt 0) { return $curves }
    $chunk = $Text.Substring($start)
    $parts = [regex]::Split($chunk, "`r?`n  - serializedVersion: 2`r?`n")
    for ($i = 1; $i -lt $parts.Length; $i++) {
        $block = $parts[$i]
        if ($block -notmatch "`r?`n    attribute: (.+)`r?`n") { continue }
        $attr = $Matches[1].Trim()
        $keys = New-Object System.Collections.Generic.List[object]
        $rx = [regex]"time: ([0-9.eE+\-]+)`r?`n        value: ([0-9.eE+\-]+)"
        foreach ($m in $rx.Matches($block)) {
            $keys.Add([pscustomobject]@{ T = [double]$m.Groups[1].Value; V = [double]$m.Groups[2].Value })
        }
        if ($keys.Count -gt 0) { $curves[$attr] = ($keys | Sort-Object T) }
    }
    return $curves
}

function Parse-QuatCurves([string]$Text) {
    $keys = New-Object System.Collections.Generic.List[object]
    $start = $Text.IndexOf("m_RotationCurves:")
    if ($start -lt 0) { return @() }
    $chunk = $Text.Substring($start)
    $end = $chunk.IndexOf("m_Compressed:")
    if ($end -lt 0) { $end = $chunk.IndexOf("m_EulerCurves:") }
    if ($end -gt 0) { $chunk = $chunk.Substring(0, $end) }
    $rx = [regex]"time: ([0-9.eE+\-]+)`r?`n        value: \{x: ([0-9.eE+\-]+), y: ([0-9.eE+\-]+), z: ([0-9.eE+\-]+), w: ([0-9.eE+\-]+)\}"
    foreach ($m in $rx.Matches($chunk)) {
        $keys.Add([pscustomobject]@{
            T = [double]$m.Groups[1].Value
            X = [double]$m.Groups[2].Value; Y = [double]$m.Groups[3].Value
            Z = [double]$m.Groups[4].Value; W = [double]$m.Groups[5].Value
        })
    }
    return ($keys | Sort-Object T)
}

function Sample1D($Keys, [double]$T) {
    if (-not $Keys -or $Keys.Count -eq 0) { return 0.0 }
    if ($T -le $Keys[0].T) { return $Keys[0].V }
    if ($T -ge $Keys[-1].T) { return $Keys[-1].V }
    for ($i = 0; $i -lt $Keys.Count - 1; $i++) {
        $t0 = $Keys[$i].T; $v0 = $Keys[$i].V
        $t1 = $Keys[$i + 1].T; $v1 = $Keys[$i + 1].V
        if ($T -ge $t0 -and $T -le $t1) {
            if ($t1 -eq $t0) { return $v1 }
            $a = ($T - $t0) / ($t1 - $t0)
            return $v0 + ($v1 - $v0) * $a
        }
    }
    return $Keys[-1].V
}

function SampleQuat($Keys, [double]$T) {
    if (-not $Keys -or $Keys.Count -eq 0) { return @(0, 0, 0, 1) }
    if ($T -le $Keys[0].T) { return @($Keys[0].X, $Keys[0].Y, $Keys[0].Z, $Keys[0].W) }
    if ($T -ge $Keys[-1].T) { return @($Keys[-1].X, $Keys[-1].Y, $Keys[-1].Z, $Keys[-1].W) }
    for ($i = 0; $i -lt $Keys.Count - 1; $i++) {
        $t0 = $Keys[$i].T; $t1 = $Keys[$i + 1].T
        if ($T -ge $t0 -and $T -le $t1) {
            $a = if ($t1 -eq $t0) { 1.0 } else { ($T - $t0) / ($t1 - $t0) }
            $x = $Keys[$i].X + ($Keys[$i + 1].X - $Keys[$i].X) * $a
            $y = $Keys[$i].Y + ($Keys[$i + 1].Y - $Keys[$i].Y) * $a
            $z = $Keys[$i].Z + ($Keys[$i + 1].Z - $Keys[$i].Z) * $a
            $w = $Keys[$i].W + ($Keys[$i + 1].W - $Keys[$i].W) * $a
            $n = [double][math]::Sqrt($x * $x + $y * $y + $z * $z + $w * $w)
            if ($n -lt 1e-8) { return @($Keys[$i + 1].X, $Keys[$i + 1].Y, $Keys[$i + 1].Z, $Keys[$i + 1].W) }
            return @([double]($x / $n), [double]($y / $n), [double]($z / $n), [double]($w / $n))
        }
    }
    return @($Keys[-1].X, $Keys[-1].Y, $Keys[-1].Z, $Keys[-1].W)
}

function Write-Array([string]$Name, [double[]]$Vals) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("        private static readonly float[] $Name =")
    [void]$sb.AppendLine("        {")
    for ($i = 0; $i -lt $Vals.Length; $i++) {
        if ($i % 8 -eq 0) { [void]$sb.Append("            ") }
        [void]$sb.Append(([string]::Format([System.Globalization.CultureInfo]::InvariantCulture, "{0:0.0000000}f", $Vals[$i])))
        if ($i -lt $Vals.Length - 1) { [void]$sb.Append(", ") }
        if ($i % 8 -eq 7 -or $i -eq $Vals.Length - 1) { [void]$sb.AppendLine() }
    }
    [void]$sb.AppendLine("        };")
    [void]$sb.AppendLine()
    return $sb.ToString()
}

$allPx = New-Object System.Collections.Generic.List[double]
$allPy = New-Object System.Collections.Generic.List[double]
$allPz = New-Object System.Collections.Generic.List[double]
$allRx = New-Object System.Collections.Generic.List[double]
$allRy = New-Object System.Collections.Generic.List[double]
$allRz = New-Object System.Collections.Generic.List[double]
$allRw = New-Object System.Collections.Generic.List[double]

foreach ($name in $Names) {
    $path = Resolve-Clip $name
    $text = [System.IO.File]::ReadAllText($path)
    $floats = Parse-FloatCurves $text
    $quats = Parse-QuatCurves $text
    for ($f = 0; $f -lt $FrameCount; $f++) {
        $t = $f / $Fps
        $allPx.Add((Sample1D $floats["m_LocalPosition.x"] $t))
        $allPy.Add((Sample1D $floats["m_LocalPosition.y"] $t))
        $allPz.Add((Sample1D $floats["m_LocalPosition.z"] $t))
        $q = SampleQuat $quats $t
        $allRx.Add($q[0]); $allRy.Add($q[1]); $allRz.Add($q[2]); $allRw.Add($q[3])
    }
    $i0 = $allPx.Count - $FrameCount
    Write-Host "$name from $(Split-Path -Leaf $path) p0=$($allPx[$i0]),$($allPy[$i0]),$($allPz[$i0]) pEnd=$($allPx[-1]),$($allPy[-1]),$($allPz[-1])"
}

$namesCs = ($Names | ForEach-Object { "`"$_`"" }) -join ", "
$out = @"
// AUTO-GENERATED by UnityBake/gen_sledgepin_cube_keys.ps1 — do not edit.
// Crosswim-style absolute local pose per fin frame. Driver sets pos+rot directly.
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinCubeKeys
    {
        internal const int FinCount = $($Names.Length);
        internal const int FrameCount = $FrameCount;
        internal const float Fps = ${Fps}f;
        internal static readonly string[] Names = { $namesCs };

$(Write-Array "Px" $allPx.ToArray())$(Write-Array "Py" $allPy.ToArray())$(Write-Array "Pz" $allPz.ToArray())$(Write-Array "Rx" $allRx.ToArray())$(Write-Array "Ry" $allRy.ToArray())$(Write-Array "Rz" $allRz.ToArray())$(Write-Array "Rw" $allRw.ToArray())        private static int Index(int fin, int frame) => fin * FrameCount + frame;

        internal static Vector3 SamplePos(int fin, float frame)
        {
            if (fin < 0 || fin >= FinCount)
                return Vector3.zero;
            float f = frame;
            if (f < 0f) f = 0f;
            float last = FrameCount - 1;
            if (f > last) f = last;
            int i0 = (int)f;
            int i1 = i0 + 1;
            if (i1 >= FrameCount) i1 = FrameCount - 1;
            float t = f - i0;
            int a = Index(fin, i0);
            int b = Index(fin, i1);
            return Vector3.Lerp(new Vector3(Px[a], Py[a], Pz[a]), new Vector3(Px[b], Py[b], Pz[b]), t);
        }

        internal static Quaternion SampleRot(int fin, float frame)
        {
            if (fin < 0 || fin >= FinCount)
                return Quaternion.identity;
            float f = frame;
            if (f < 0f) f = 0f;
            float last = FrameCount - 1;
            if (f > last) f = last;
            int i0 = (int)f;
            int i1 = i0 + 1;
            if (i1 >= FrameCount) i1 = FrameCount - 1;
            float t = f - i0;
            int a = Index(fin, i0);
            int b = Index(fin, i1);
            Quaternion qa = new Quaternion(Rx[a], Ry[a], Rz[a], Rw[a]);
            Quaternion qb = new Quaternion(Rx[b], Ry[b], Rz[b], Rw[b]);
            return Quaternion.Slerp(qa, qb, t);
        }
    }
}
"@

[System.IO.File]::WriteAllText($OutCs, $out, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $OutCs"
