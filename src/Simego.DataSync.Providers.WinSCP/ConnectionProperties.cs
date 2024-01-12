using Simego.DataSync.Providers.WinSCP.TypeEditors;
using System.ComponentModel;
using System.Drawing.Design;

namespace Simego.DataSync.Providers.WinSCP
{
    class ConnectionProperties
    {
        private readonly WinSCPDatasourceReader _reader;

        [Category("Connection"), Description("The FTP Protocol to use either FTP or SFTP.")]
        public WinSCPProtocol Protocol { get => _reader.Protocol; set => _reader.Protocol = value; }

        [Category("Connection"), Description("The FTP Server Host")]
        public string HostName { get => _reader.HostName; set => _reader.HostName = value; }

        [Category("Connection"), Description("The FTP Server Host TCP Port")]
        public int PortNumber { get => _reader.PortNumber; set => _reader.PortNumber = value; }

        [Category("Connection"), Description("The base path to the files on the FTP Host.")]
        public string Path { get => _reader.Path; set => _reader.Path = value; }

        [Category("Connection.Authentication"), Description("The Username to connect.")]
        [Editor(typeof(CredentialsTypeEditor), typeof(UITypeEditor))]
        public string UserName { get => _reader.UserName; set => _reader.UserName = value; }

        [Browsable(false)]
        public string Password { get => _reader.Password; set => _reader.Password = value; }

        [Category("Filter"), Description("Standard Windows File Filter i.e. *.csv or multiple *.csv;*.txt")]
        public string FilterPattern { get => _reader.FilterPattern; set => _reader.FilterPattern = value; }

        [Category("Filter"), Description("The reciurse down the directory tree.")]
        public bool RecuirseFolders { get => _reader.RecuirseFolders; set => _reader.RecuirseFolders = value; }

        [Category("Filter"), Description("Include only files that we're modified since. Format yyyy-MM-dd HH:mm:ss")]
        public string ModifiedSinceUTC { get => _reader.ModifiedSinceUTC; set => _reader.ModifiedSinceUTC = value; }

        [Category("Filter"), Description("Return the Path as Web Friendly with Forward Slashes.")]
        public bool WebFriendlyPaths { get => _reader.WebFriendlyPaths; set => _reader.WebFriendlyPaths = value; }

        [Category("Connection.FTP"), Description("The FTP Secure mode.")]
        public WinSCPFtpSecure FtpSecure { get => _reader.FtpSecure; set => _reader.FtpSecure = value; }

        [Category("Connection.FTP"), Description("Accept the Server Certificate.")]
        public bool AcceptAnyTlsHostCertificate { get => _reader.AcceptAnyTlsHostCertificate; set => _reader.AcceptAnyTlsHostCertificate = value; }

        [Category("Connection.SFTP"), Description("Accept the Server SSH Host Key.")]
        public bool AcceptAnySshHostKey { get => _reader.AcceptAnySshHostKey; set => _reader.AcceptAnySshHostKey = value; }

        [Category("Connection.SFTP"), Description("SSH Host Key Fingerprint to accept.")]
        public string SshHostKeyFingerprint { get => _reader.SshHostKeyFingerprint; set => _reader.SshHostKeyFingerprint = value; }
        
        [Category("Connection.SFTP"), Description("SSH Private Key Passphrase.")]
        public string SshPrivateKeyPassphrase { get => _reader.SshPrivateKeyPassphrase; set => _reader.SshPrivateKeyPassphrase = value; }

        [Category("Connection.SFTP"), Description("SSH Private Key Path.")]
        public string SshPrivateKeyPath { get => _reader.SshPrivateKeyPath; set => _reader.SshPrivateKeyPath = value; }
        
        [Category("Writer.Options"), Description("Preserve Timestamp on write.")]
        public bool PreserveTimestamp { get => _reader.PreserveTimestamp; set => _reader.PreserveTimestamp = value; }

        public ConnectionProperties(WinSCPDatasourceReader reader)
        {
            _reader = reader;
        }        
    }
}
