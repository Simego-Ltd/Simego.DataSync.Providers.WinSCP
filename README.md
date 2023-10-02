# Simego Data Sync FTP/SFTP Connector

Simego Data Sync connector that wraps the WinSCP https://winscp.net/eng/index.php FTP/SFTP component.

To install this connector you will need to build it then copy the output dlls **Simego.DataSync.Providers.WinSCP.dll**, **WinSCPnet.dll** and **WinSCP.exe** to the Data Sync installation folder.

You can build without Visual Studio using MSBuild via ``MSBuild.exe /t:Build /p:Configuration=Release``

![Connector Screenshot](https://github.com/Simego-Ltd/datasync-winscp-connector/blob/main/screenshot-1.png "Connector Screenshot")


## Install the Connector 

You need to start by installing the WinScp connector. We have built a connector installer inside Data Sync that will download and install the relevant files for you.

To get to the installer go to open the File menu and select **Install Data Connector**.

![install-data-connector](https://github.com/Simego-Ltd/Simego.DataSync.Providers.WinSCP/assets/63856275/bd1d27e2-5d24-4834-8264-637fba2020c2)


This will open the Connector Installer Window, where all you need to do is select **WinSCP** from the drop down and click **OK**.



![install-winscp-connector](https://github.com/Simego-Ltd/Simego.DataSync.Providers.WinSCP/assets/63856275/56eab9f0-93bd-4bed-920b-17eb5775eaa7)

If the installation was successful you should get a confirmation popup and you now need to close all instances of Data Sync and then re-start the program. 

![installed-connector-successfully](https://github.com/Simego-Ltd/Simego.DataSync.Providers.WinSCP/assets/63856275/0d57a569-cdcf-4435-850d-40c894da1967)


>If you get an error saying it could not install the connector because it is "Unable to access folder", then this is because the folder is locked by Ouvvi. 
>Please stop your Ouvvi services and try again.

You can now access the connector from the connection window by expanding the **File System** folder and selecting **WinSCP FTP/SFTP**.

![winscp-connection-blank](https://github.com/Simego-Ltd/Simego.DataSync.Providers.WinSCP/assets/63856275/3e92c9d2-4396-4549-aa71-58ca8350cdc2)

You can now complete the connection with the details to your file server and click **Connect** to load the datasource into the window.

## Using the Connector

To connect you need to enter in the HostName, this might be the a URL or IP address, a path to a directory e.g. "documents", and any credentials needed to access the server.

You can add the credentials by clicking onto the ellipsis in the UserName field and then enter in the username and password into the window.

If your server requires additional settings for the connection you can add these now.

Once you are done click **Connect**.

![winscp-connection-usercred](https://github.com/Simego-Ltd/Simego.DataSync.Providers.WinSCP/assets/63856275/a41a8460-7ab0-47bd-8990-6e7787b86bfe)

This will load the columns and connection details into the datasource window. 
You can then add and remove columns to the schema map, and preview the data to ensure the connection is working by clicking the **Preview** button.

![winscp-preview-data](https://github.com/Simego-Ltd/Simego.DataSync.Providers.WinSCP/assets/63856275/16d676a2-20d9-4d43-8b79-bab7f43b6d7e)



