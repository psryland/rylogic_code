# Fronius Solar Inverter Monitor

This windows service polls the Fronius inverter and logs the reading in an sqlite database

## Service Install/Uninstall

In an admin powershell console:
 - Create the service:
    ```
    New-Service
        -Name "FroniusMonitor"
        -DisplayName "Fronius Monitor"
        -BinaryPathName "D:\SolarHotWater\Fronius\FroniusMonitor.Service.exe"
        -Description "Solar output data logging"
        -StartupType Automatic
    ```
    The service will run under the local system account. Make sure the appsettings.json has the correct UserProfile name
    so that the fronius.db file is in the correct place.

 - Start the Service:
    ```Start-Service -Name "FroniusMonitor"

 - Remove the service
    ```Remove-Service -Name "FroniusMonitor"

Check the service actually starts. Start-Service doesn't report an error if the service stops again (due to a crash etc).
If the service fails to start, run the 'FroniusMonitor.Service.exe' manually at the command line.
