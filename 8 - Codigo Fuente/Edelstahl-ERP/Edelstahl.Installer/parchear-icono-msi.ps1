param([string]$msi, [string]$ico = 'C:\pp3\clon-subida\repo\8 - Codigo Fuente\Edelstahl-ERP\Edelstahl.WinApp\Resources\Icono\edelstahl.ico', [switch]$parchear)
$ErrorActionPreference = 'Stop'
$inst = New-Object -ComObject WindowsInstaller.Installer
function Inv($o, $m, $t, $a) { $o.GetType().InvokeMember($m, $t, $null, $o, $a) }
$modo = if ($parchear) { 1 } else { 0 }
$db = Inv $inst 'OpenDatabase' 'InvokeMethod' @($msi, [int]$modo)
function Filas($sql, [int]$cols) {
  $v = Inv $db 'OpenView' 'InvokeMethod' @($sql); [void](Inv $v 'Execute' 'InvokeMethod' $null)
  $res = @()
  while ($r = Inv $v 'Fetch' 'InvokeMethod' $null) { $f = @(); for ($i = 1; $i -le $cols; $i++) { $f += [string](Inv $r 'StringData' 'GetProperty' @([int]$i)) }; $res += , ($f -join ' | ') }
  [void](Inv $v 'Close' 'InvokeMethod' $null); return $res
}
"Icon:       " + ((Filas 'SELECT Name FROM Icon' 1) -join ' ; ')
"Shortcut:   " + ((Filas 'SELECT Shortcut, Icon_, IconIndex FROM Shortcut' 3) -join ' ; ')
"ARP icono:  " + ((Filas "SELECT Property, Value FROM Property WHERE Property='ARPPRODUCTICON'" 2) -join ' ; ')
if ($parchear) {
  $viejos = @(Filas 'SELECT Name FROM Icon' 1)
  $nuevo = 'edelstahl.ico'
  $rec = Inv $inst 'CreateRecord' 'InvokeMethod' @([int]2)
  [void](Inv $rec 'StringData' 'SetProperty' @([int]1, $nuevo))
  [void](Inv $rec 'SetStream' 'InvokeMethod' @([int]2, $ico))
  $v = Inv $db 'OpenView' 'InvokeMethod' @('INSERT INTO `Icon` (`Name`, `Data`) VALUES (?, ?)'); [void](Inv $v 'Execute' 'InvokeMethod' @($rec)); [void](Inv $v 'Close' 'InvokeMethod' $null)
  $sqls = @("UPDATE ``Property`` SET ``Value``='$nuevo' WHERE ``Property``='ARPPRODUCTICON'")
  foreach ($viejo in $viejos) { $sqls += "UPDATE ``Shortcut`` SET ``Icon_``='$nuevo' WHERE ``Icon_``='$viejo'" }
  foreach ($viejo in $viejos) { $sqls += "DELETE FROM ``Icon`` WHERE ``Name``='$viejo'" }
  foreach ($sql in $sqls) {
    $v = Inv $db 'OpenView' 'InvokeMethod' @($sql); [void](Inv $v 'Execute' 'InvokeMethod' $null); [void](Inv $v 'Close' 'InvokeMethod' $null)
  }
  [void](Inv $db 'Commit' 'InvokeMethod' $null)
  "--- parcheado ---"
  "Icon:       " + ((Filas 'SELECT Name FROM Icon' 1) -join ' ; ')
  "Shortcut:   " + ((Filas 'SELECT Shortcut, Icon_, IconIndex FROM Shortcut' 3) -join ' ; ')
  "ARP icono:  " + ((Filas "SELECT Property, Value FROM Property WHERE Property='ARPPRODUCTICON'" 2) -join ' ; ')
}
