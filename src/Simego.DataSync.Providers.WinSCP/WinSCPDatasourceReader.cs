using Microsoft.VisualBasic.CompilerServices;
using Simego.DataSync.Engine;
using Simego.DataSync.Interfaces;
using Simego.DataSync.Providers.WinSCP.TypeEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing.Design;
using System.Globalization;
using System.Windows.Forms;
using WinSCP;

namespace Simego.DataSync.Providers.WinSCP
{
    [ProviderInfo(Name = "WinSCP FTP/SFTP", Group = "FileSystem", Description = "Data Sync Connector for WinSCP https://winscp.net/ FTP and SFTP file transfers.")]
    public class WinSCPDatasourceReader : DataReaderProviderBase, IDataSourceSetup
    {
        private ConnectionInterface _connectionIf;

        [Category("Connection"), Description("The FTP Protocol to use either FTP or SFTP.")]
        public WinSCPProtocol Protocol { get; set; }
               
        [Category("Connection"), Description("The FTP Server Host")]
        public string HostName { get; set; }
        
        [Category("Connection"), Description("The FTP Server Host TCP Port")]
        public int PortNumber { get; set; }

        [Category("Connection"), Description("The base path to the files on the FTP Host.")]
        public string Path { get; set; }
        
        [Category("Connection.Authentication"), Description("The Username to connect.")]
        [Editor(typeof(CredentialsTypeEditor), typeof(UITypeEditor))]
        public string UserName { get; set; }

        [Browsable(false)]
        public string Password { get; set; }

        [Category("Filter"), Description("Standard Windows File Filter i.e. *.csv or multiple *.csv;*.txt")]
        public string FilterPattern { get; set; }
        
        [Category("Filter"), Description("The reciurse down the directory tree.")]
        public bool RecuirseFolders { get; set; }

        [Category("Filter"), Description("Include only files that we're modified since. Format yyyy-MM-dd HH:mm:ss")]
        public string ModifiedSinceUTC { get; set; }
       
        [Category("Filter"), Description("Return the Path as Web Friendly with Forward Slashes.")]
        public bool WebFriendlyPaths { get; set; }

        [Category("Connection.FTP"), Description("The FTP Secure mode.")]
        public WinSCPFtpSecure FtpSecure { get; set; }
        
        [Category("Connection.FTP"), Description("Accept the Server Certificate.")]
        public bool AcceptAnyTlsHostCertificate { get; set; }

        [Category("Connection.SFTP"), Description("Accept the Server SSH Host Key.")]
        public bool AcceptAnySshHostKey { get; set; }

        [Category("Connection.SFTP"), Description("SSH Host Key Fingerprint to accept.")]
        public string SshHostKeyFingerprint { get; set; }

        [Category("Connection.SFTP"), Description("SSH Private Key Passphrase.")]
        public string SshPrivateKeyPassphrase { get; set; }

        [Category("Connection.SFTP"), Description("SSH Private Key Path.")]
        public string SshPrivateKeyPath { get; set; }

        public WinSCPDatasourceReader()
        {
            RecuirseFolders = true;
            WebFriendlyPaths = true;
            ModifiedSinceUTC = null;
            FilterPattern = "*.*";            
        }
        
        public override DataTableStore GetDataTable(DataTableStore dt)
        {
            // Key this Data Set by the Filename
            dt.AddIdentifierColumn(typeof(string));

            var mapping = new DataSchemaMapping(SchemaMap, Side);
            var columns = SchemaMap.GetIncludedColumns();

            var modifiedSince = string.IsNullOrEmpty(ModifiedSinceUTC) ? default(DateTime?) : DateTime.SpecifyKind(DateTime.Parse(ModifiedSinceUTC), DateTimeKind.Utc);
            
            using (Session session = GetSession())
            {
                // Get the Files from the Server                
                GetFiles(session, session.ListDirectory(Path), RecuirseFolders, dt, mapping, columns, modifiedSince);                
            }            
                                    
            return dt;
        }
       
        public Session GetSession()
        {
            Session session = new Session();
            try
            {
                if(Protocol == WinSCPProtocol.SFtp && AcceptAnySshHostKey)
                {
                    try
                    {
                        System.Diagnostics.Trace.WriteLine($"SSH Host Key: {session.ScanFingerprint(GetSessionOptions(), "SHA-256")}");                        
                    }
                    catch
                    {
                        //Ignore
                    }
                }
                
                session.Open(GetSessionOptions());
                
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        internal SessionOptions GetSessionOptions()
        {
            SessionOptions sessionOptions = new SessionOptions
            {
                Protocol = (Protocol)Enum.Parse(typeof(Protocol), Protocol.ToString(), true),
                HostName = HostName,
                UserName = UserName,
                Password = SecurityService.DecyptValue(Password),
                PortNumber = PortNumber
            };

            if (Protocol == WinSCPProtocol.Ftp)
            {
                sessionOptions.FtpSecure = (FtpSecure)Enum.Parse(typeof(FtpSecure), FtpSecure.ToString(), true);
                sessionOptions.GiveUpSecurityAndAcceptAnyTlsHostCertificate = AcceptAnyTlsHostCertificate;
            }
            else if (Protocol == WinSCPProtocol.SFtp)
            {
                sessionOptions.GiveUpSecurityAndAcceptAnySshHostKey = AcceptAnySshHostKey;
                sessionOptions.SshHostKeyFingerprint = string.IsNullOrEmpty(SshHostKeyFingerprint) ? null : SshHostKeyFingerprint;                
                sessionOptions.PrivateKeyPassphrase = string.IsNullOrEmpty(SshPrivateKeyPassphrase) ? null : SecurityService.DecyptValue(SshPrivateKeyPassphrase);
                sessionOptions.SshPrivateKeyPath = string.IsNullOrEmpty(SshPrivateKeyPath) ? null : SshPrivateKeyPath;
            }

            return sessionOptions;
        }

        private void GetFiles(Session session, RemoteDirectoryInfo directoryInfo, bool recuirse, DataTableStore dt, DataSchemaMapping mapping, IList<DataSchemaItem> columns, DateTime? modifiedSince)
        {
            if (directoryInfo == null) return;
            if (dt.ContinueLoad != null && !dt.ContinueLoad(0)) return;

            foreach (RemoteFileInfo file in directoryInfo.Files)
            {
                if (file.IsDirectory)
                {
                    if (recuirse)
                    {
                        if (file.IsParentDirectory == false && file.IsThisDirectory == false)
                        {
                            var folder = session.ListDirectory(file.FullName);
                            if (folder != null)
                            {
                                GetFiles(session, folder, recuirse, dt, mapping, columns, modifiedSince);
                            }
                        }
                    }
                }
                else
                {
                    var fileName = Utility.StripStartSlash(file.FullName.Substring(Path.Length));
                    var fileModified = file.LastWriteTime.ToUniversalTime();

                    var newRow = dt.NewRow();

                    foreach (DataSchemaItem item in columns)
                    {
                        string columnName = mapping.MapColumnToDestination(item);
                        
                        switch (columnName)
                        {
                            case "FullFileName":
                                {                                                                        
                                    if (WebFriendlyPaths)
                                    {
                                        newRow[item.ColumnName] = fileName != null ? DataSchemaTypeConverter.ConvertTo<string>(fileName).Replace("\\", "/") : fileName;
                                    }
                                    else
                                    {
                                        newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(fileName, item.DataType);
                                    }                                   

                                    break;
                                }
                            case "Path":
                                {                                    
                                    string path = Utility.StripStartSlash(System.IO.Path.GetDirectoryName(file.FullName).Substring(Path.Length));

                                    if (WebFriendlyPaths)
                                    {
                                        newRow[item.ColumnName] = path != null ? DataSchemaTypeConverter.ConvertTo<string>(path).Replace("\\", "/") : path;
                                    }
                                    else
                                    {
                                        newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(path, item.DataType);
                                    }

                                    break;
                                }
                            case "FileName":
                                {
                                    newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(file.Name, item.DataType);
                                    break;
                                }
                            case "DateCreated":
                            case "DateModified":
                                {
                                    newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(fileModified, item.DataType);
                                    break;
                                }
                            case "Length":
                                {
                                    newRow[item.ColumnName] = DataSchemaTypeConverter.ConvertTo(file.Length, item.DataType);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                    }

                    if (LikeOperator.LikeString(fileName, FilterPattern, Microsoft.VisualBasic.CompareMethod.Binary))
                    {
                        if (modifiedSince != null && modifiedSince.HasValue)
                        {
                            if (fileModified >= modifiedSince)
                            {
                                dt.Rows.AddWithIdentifier(newRow, file.FullName);
                            }
                        }
                        else
                        {
                            dt.Rows.AddWithIdentifier(newRow, file.FullName);
                        }
                    }                    
                }
            }
        }
       
        public override DataSchema GetDefaultDataSchema()
        {
            DataTable dt = new DataTable() { TableName = "WinSCP" };

            dt.Columns.Add(CreateStringColumn("FullFileName", 260, true, false));
            dt.Columns.Add(CreateStringColumn("Path", 260, false, false));
            dt.Columns.Add(CreateStringColumn("FileName", 260, false, false));
            dt.Columns.Add(CreateColumn("DateCreated", typeof(DateTime), false));
            dt.Columns.Add(CreateColumn("DateModified", typeof(DateTime), false));
            dt.Columns.Add(CreateColumn("Length", typeof(long), false));
            
            return new DataSchema(dt);
        }

        public override List<ProviderParameter> GetInitializationParameters()
        {
            return new List<ProviderParameter>
            {
                new ProviderParameter("Path", Path, GetConfigKey("Path")),
                new ProviderParameter("FilterPattern", FilterPattern, GetConfigKey("FilterPattern")),
                new ProviderParameter("ModifiedSinceUTC", ModifiedSinceUTC, GetConfigKey("ModifiedSinceUTC")),
                new ProviderParameter("RecuirseFolders", RecuirseFolders.ToString(), GetConfigKey("RecuirseFolders")),
                new ProviderParameter("WebFriendlyPaths", WebFriendlyPaths.ToString(), GetConfigKey("WebFriendlyPaths")),
                new ProviderParameter("Protocol", Protocol.ToString(), GetConfigKey("Protocol")),
                new ProviderParameter("HostName", HostName, GetConfigKey("HostName")),
                new ProviderParameter("PortNumber", PortNumber.ToString(CultureInfo.InvariantCulture), GetConfigKey("HostName")),
                new ProviderParameter("UserName", UserName, GetConfigKey("UserName")),
                new ProviderParameter("Password", SecurityService.EncryptValue(Password), GetConfigKey("Password")),
                new ProviderParameter("FtpSecure", FtpSecure.ToString(), GetConfigKey("FtpSecure")),
                new ProviderParameter("AcceptAnyTlsHostCertificate", AcceptAnyTlsHostCertificate.ToString(CultureInfo.InvariantCulture), GetConfigKey("AcceptAnyTlsHostCertificate")),
                new ProviderParameter("AcceptAnySshHostKey", AcceptAnySshHostKey.ToString(CultureInfo.InvariantCulture), GetConfigKey("AcceptAnySshHostKey")),
                new ProviderParameter("SshHostKeyFingerprint", SshHostKeyFingerprint, GetConfigKey("SshHostKeyFingerprint")),
                new ProviderParameter("SshPrivateKeyPassphrase", SecurityService.EncryptValue(SshPrivateKeyPassphrase), GetConfigKey("SshPrivateKeyPassphrase")),
                new ProviderParameter("SshPrivateKeyPath", SshPrivateKeyPath, GetConfigKey("SshPrivateKeyPath"))
            };
        }

        public override void Initialize(List<ProviderParameter> parameters)
        {
            foreach (ProviderParameter p in parameters)
            {
                AddConfigKey(p.Name, p.ConfigKey);

                switch (p.Name)
                {
                    case "Protocol":
                        {
                            if(Enum.TryParse(p.Value, true, out WinSCPProtocol value))
                            {
                                Protocol = value;
                            }                            
                            break;
                        }
                    case "HostName":
                        {
                            HostName = p.Value;
                            break;
                        }
                    case "PortNumber":
                        {
                            if (int.TryParse(p.Value, out int val))
                            {
                                PortNumber = val;
                            }
                            break;
                        }
                    case "UserName":
                        {
                            UserName = p.Value;
                            break;
                        }
                    case "Password":
                        {
                            Password = p.Value;
                            break;
                        }
                    case "Path":
                        {
                            Path = p.Value;
                            break;
                        }
                    case "FilterPattern":
                        {
                            FilterPattern = p.Value;
                            break;
                        }
                    case "RecuirseFolders":
                        {
                            if (bool.TryParse(p.Value, out bool val))
                            {
                                RecuirseFolders = val;
                            }
                            break;
                        }
                    case "WebFriendlyPaths":
                        {
                            if (bool.TryParse(p.Value, out bool val))
                            {
                                WebFriendlyPaths = val;
                            }
                            break;
                        }
                    case "ModifiedSinceUTC":
                        {
                            ModifiedSinceUTC = p.Value;
                            break;
                        }

                    case "FtpSecure":
                        {
                            if (Enum.TryParse(p.Value, true, out WinSCPFtpSecure value))
                            {
                                FtpSecure = value;
                            }
                            break;
                        }
                    case "AcceptAnyTlsHostCertificate":
                        {
                            if (bool.TryParse(p.Value, out bool val))
                            {
                                AcceptAnyTlsHostCertificate = val;
                            }
                            break;
                        }
                    case "AcceptAnySshHostKey":
                        {
                            if (bool.TryParse(p.Value, out bool val))
                            {
                                AcceptAnySshHostKey = val;
                            }
                            break;
                        }
                    case "SshHostKeyFingerprint":
                        {
                            SshHostKeyFingerprint = p.Value;
                            break;
                        }
                    case "SshPrivateKeyPassphrase":
                        {
                            SshPrivateKeyPassphrase = p.Value;
                            break;
                        }
                    case "SshPrivateKeyPath":
                        {
                            SshPrivateKeyPath = p.Value;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }

        public override IDataSourceWriter GetWriter() => new WinSCPDataSourceWriter { SchemaMap = SchemaMap };

        public override byte[] GetBlobData(DataCompareItem item, int index)
        {
            string fileName = item.GetSourceIdentifier<string>(0);
            string tmpFile = System.IO.Path.Combine(Utility.GetTempPath(), $"{Guid.NewGuid()}.tmp");

            try
            {
                //TODO: Need a change here to Data Sync so we can terminate the session and stop WinSCP.exe after the sync is finished rather than on each file.

                using (Session session = GetSession())
                {
                    var result = session.GetFiles(fileName, tmpFile, false, new TransferOptions { TransferMode = TransferMode.Binary });

                    if (result.IsSuccess)
                        return System.IO.File.ReadAllBytes(tmpFile);

                    throw new Exception($"ERROR: Downloading File ({fileName})");
                }

            }
            catch (OverflowException)
            {
                throw new ArgumentException($"ERROR: File ({fileName}) is too large to synchronise");
            }
            finally
            {
                if(System.IO.File.Exists(tmpFile))
                {
                    try
                    {
                        System.IO.File.Delete(tmpFile);
                    }
                    catch (System.IO.IOException)
                    {
                        // Ignore ...
                    }
                }
            }
        }

        public override string GetFileName(DataCompareItem item, int index) => System.IO.Path.GetFileName(item.GetSourceIdentifier<string>(0));

        public override string GetFilePath(DataCompareItem item, int index) => Utility.StripStartSlash(System.IO.Path.GetDirectoryName(item.GetSourceIdentifier<string>(0)).Substring(Path.Length));

        #region IDataSourceSetup - Render Custom Configuration UI

        public void DisplayConfigurationUI(Control parent)
        {
            if (_connectionIf == null)
            {
                _connectionIf = new ConnectionInterface() {
                    Font = parent.Font,
                    Dock = DockStyle.Fill,                    
                };
                _connectionIf.PropertyGrid.SelectedObject = new ConnectionProperties(this);
            }
            
            parent.Controls.Add(_connectionIf);
        }

        public bool Validate() => true;

        public IDataSourceReader GetReader() => this;

        #endregion

        private DataColumn CreateStringColumn(string name, int length, bool unique, bool allowNull)
        {
            var dc = new DataColumn(name, typeof(string))
            {
                MaxLength = length,
                Unique = unique,
                AllowDBNull = allowNull
            };

            return dc;
        }

        private DataColumn CreateColumn(string name, Type t, bool allowNull)
        {
            var dc = new DataColumn(name, t) { AllowDBNull = allowNull };

            return dc;
        }
    }
}
