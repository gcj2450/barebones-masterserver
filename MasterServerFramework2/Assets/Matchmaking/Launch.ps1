# A PowerShell script to start a set of clients in a grid
#
# Credit to the following article
# https://www.windrath.com/2016/05/powershell-start-process-wrapper/
# example call
# Start-ProcessAndSetWindow .\EmilClient.exe -PosX 100 -PosY 400

$application = ".\Matchmaking.exe"
$maxcolumns = 4
$maxrows = 2
$height = 300
$width = 400
$args = "-client"
$sleepduration = 1000
 

function global:Start-ProcessAndSetWindow() 
{
    [CmdletBinding()]
    param 
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string] $FilePath,
 
        [Parameter(Mandatory = $true)]
        [int] $PosX,
 
        [Parameter(Mandatory = $true)]
        [int] $PosY,
 
        [Parameter(Mandatory = $false)]
        [int] $Height = -1,
 
        [Parameter(Mandatory = $false)]
        [int] $Width = -1,
         
        [Parameter(Mandatory = $false, ValueFromRemainingArguments=$true)]
        $StartProcessParameters
    )
    
    # Invoke process
    
    $process = [System.Diagnostics.Process]::Start("$FilePath", "$StartProcessParameters")

    # We need to get the process' MainWindowHandle. That's not the processes handle or Id!
    if($process -is [System.Array]) { $procId = $process[0].Id } else { $procId = $process.Id }
 
    # ... fallback in case something goes south (wait up to 5 seconds for the process to launch)
    $i = 50  
 
    # ... Start looking for the main window handle. May take a bit of time for the window to show up
    $mainWindowHandle = [System.IntPtr]::Zero
    while($mainWindowHandle -eq [System.IntPtr]::Zero)
    {
       [System.Threading.Thread]::Sleep(100)
       $tmp = Get-Process -Id $procId -ErrorAction SilentlyContinue
 
       if($tmp -ne $null) 
       {
         $mainWindowHandle = $tmp.MainWindowHandle
       }
 
       $i = $i - 1
       if($i -le 0)
       {
         break
       }
    }
    
    # Once we grabbed the MainWindowHandle, we need to use the Win32-API function SetWindowPosition (using inline C#)
    if($mainWindowHandle -ne [System.IntPtr]::Zero)
    {
        $CSharpSource = @" 
            using System; 
            using System.Runtime.InteropServices;
 
            namespace TW.Tools.InlinePS
            {
                public static class WindowManagement
                {
                    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
                    public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
                    
                    public const int SWP_NOSIZE = 0x01, SWP_NOMOVE = 0x02, SWP_SHOWWINDOW = 0x40, SWP_HIDEWINDOW = 0x80;
 
                    public static void SetPosition(IntPtr handle, int x, int y, int width, int height)
                    {
                        if (handle != null)
                        { 
                            SetWindowPos(handle, 0, x, y, 0, 0, SWP_NOSIZE | SWP_HIDEWINDOW);
                
                            if (width > -1 && height > -1)
                                SetWindowPos(handle, 0, 0, 0, width, height, SWP_NOMOVE);
 
                            SetWindowPos(handle, 0, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
                        }
                    }
                }
            }
"@ 
 
        Add-Type -TypeDefinition $CSharpSource -Language CSharp -ErrorAction SilentlyContinue
        [TW.Tools.InlinePS.WindowManagement]::SetPosition($mainWindowHandle, $PosX, $PosY, $Width, $Height);
    }
    else
    {
      throw "Couldn't find the MainWindowHandle, aborting (your process should be still alive)"
    }
}

for($column = 0; $column -lt $maxcolumns; $column++)
{
    $x = $width * $column
    for($row = 0; $row -lt $maxrows; $row++)
    {
	$y = $height * $row
       
	Start-ProcessAndSetWindow $application -PosX $x -PosY $y $args
        [System.Threading.Thread]::Sleep($sleepduration)
  }
}