# Simego Data Sync FTP/SFTP Connector

Simego Data Sync connector that wraps the WinSCP https://winscp.net/eng/index.php FTP/SFTP component.

To install this connector you will need to build it then copy the output dlls **Simego.DataSync.Providers.WinSCP.dll**, **WinSCPnet.dll** and **WinSCP.exe** to the Data Sync installation folder.

You can build without Visual Studio using MSBuild via ``MSBuild.exe /t:Build /p:Configuration=Release``

