using FluentFTP;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CIAPI.Library.Helpers
{
    /// <summary>
    /// Clase para gestionar operaciones FTP, FTPS y SFTP (por defecto FTP).
    /// </summary>
    public class FTPManager : IDisposable
    {
        public enum FtpMode { FTP, FTPS, SFTP }

        private readonly string _server;
        private readonly string _user;
        private readonly string _password;
        private readonly FtpMode _mode;
        private string _currentDirectory = "/";
        private FtpClient _ftpClient;
        private SftpClient _sftpClient;

        public FTPManager(string server, string user, string password, FtpMode mode = FtpMode.FTP)
        {
            _server = server.Replace("ftp://", "").Replace("ftps://", "").Replace("sftp://", "").TrimEnd('/');
            _user = user;
            _password = password;
            _mode = mode;
        }

        public void Connect()
        {
            if (_mode == FtpMode.FTP || _mode == FtpMode.FTPS)
            {
                if (_ftpClient == null)
                {
                    _ftpClient = new FtpClient(_server, _user, _password);

                    if (_mode == FtpMode.FTPS)
                    {
                        // Habilitar FTPS (SSL/TLS)
                        _ftpClient.Config.EncryptionMode = FtpEncryptionMode.Explicit; // Usa TLS explícito (puerto 21)
                        _ftpClient.Config.DataConnectionEncryption = true;
                        // Opcional: aceptar cualquier certificado (no recomendado en producción)
                        _ftpClient.ValidateCertificate += (control, e) => { e.Accept = true; };
                    }
                }
                if (!_ftpClient.IsConnected)
                    _ftpClient.Connect();
            }
            else if (_mode == FtpMode.SFTP)
            {
                if (_sftpClient == null)
                    _sftpClient = new SftpClient(_server, _user, _password);
                if (!_sftpClient.IsConnected)
                    _sftpClient.Connect();
            }
        }

        public void Disconnect()
        {
            if (_ftpClient != null && _ftpClient.IsConnected)
                _ftpClient.Disconnect();
            if (_sftpClient != null && _sftpClient.IsConnected)
                _sftpClient.Disconnect();
        }

        public void ChangeDirectory(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("El path remoto no puede ser vacío.");
            _currentDirectory = remotePath.StartsWith("/") ? remotePath : "/" + remotePath;
        }

        public List<string> ListDirectory()
        {
            Connect();
            if (_mode == FtpMode.FTP || _mode == FtpMode.FTPS)
            {
                var items = _ftpClient.GetListing(_currentDirectory);
                return items.Select(x => x.Name).ToList();
            }
            else // SFTP
            {
                var files = _sftpClient.ListDirectory(_currentDirectory);
                return files.Select(f => f.Name).ToList();
            }
        }

        /// <summary>
        /// Obtiene la fecha de modificación y el tamaño (bytes) de un archivo remoto.
        /// Devuelve una tupla (LastModified, Size). Si no se puede obtener alguno de los valores retorna null para ese valor.
        /// </summary>
        /// <param name="remoteFileName">Nombre del archivo en el directorio actual remoto (o ruta relativa)</param>
        /// <returns>(DateTime? LastModified, long? Size)</returns>
        public (DateTime? LastModified, long? Size) GetFileInfo(string remoteFileName)
        {
            if (string.IsNullOrWhiteSpace(remoteFileName))
                throw new ArgumentException("remoteFileName no puede estar vacío.", nameof(remoteFileName));

            Connect();
            var remoteUri = CombinePath(_currentDirectory, remoteFileName);

            try
            {
                if (_mode == FtpMode.FTP || _mode == FtpMode.FTPS)
                {
                    DateTime? last = null;
                    long? size = null;

                    // Intentamos obtener la fecha de modificación (puede lanzar en algunos servidores)
                    try
                    {
                        // FluentFTP: GetModifiedTime acepta ruta remota
                        last = _ftpClient.GetModifiedTime(remoteUri);
                    }
                    catch
                    {
                        // Ignorar y dejar null
                        last = null;
                    }

                    // Intentamos obtener el tamaño
                    try
                    {
                        size = _ftpClient.GetFileSize(remoteUri);
                    }
                    catch
                    {
                        size = null;
                    }

                    return (last, size);
                }
                else // SFTP
                {
                    try
                    {
                        var attr = _sftpClient.GetAttributes(remoteUri);
                        DateTime? last = attr.LastWriteTime;
                        long? size = attr.Size;
                        return (last, size);
                    }
                    catch
                    {
                        return (null, null);
                    }
                }
            }
            catch
            {
                return (null, null);
            }
        }

        public bool FileExists(string remoteFileName)
        {
            Connect();
            var remoteUri = CombinePath(_currentDirectory, remoteFileName);
            if (_mode == FtpMode.FTP || _mode == FtpMode.FTPS)
                return _ftpClient.FileExists(remoteUri);
            else
                return _sftpClient.Exists(remoteUri);
        }

        public void DownloadFile(string remoteFileName, string localPath)
        {
            Connect();
            var remoteUri = CombinePath(_currentDirectory, remoteFileName);
            if (_mode == FtpMode.FTP || _mode == FtpMode.FTPS)
            {
                _ftpClient.DownloadFile(localPath, remoteUri, FtpLocalExists.Overwrite, FtpVerify.None);
            }
            else // SFTP
            {
                using (var fs = new FileStream(localPath, FileMode.Create))
                {
                    _sftpClient.DownloadFile(remoteUri, fs);
                }
            }
        }

        public void DownloadFiles(IEnumerable<string> remoteFileNames, string localFolder)
        {
            foreach (var file in remoteFileNames)
            {
                var localPath = Path.Combine(localFolder, file);
                DownloadFile(file, localPath);
            }
        }

        public void UploadFile(string localFilePath, string remoteFileName = null)
        {
            Connect();
            var fileName = remoteFileName ?? Path.GetFileName(localFilePath);
            var remoteUri = CombinePath(_currentDirectory, fileName);

            if (_mode == FtpMode.FTP || _mode == FtpMode.FTPS)
            {
                _ftpClient.UploadFile(localFilePath, remoteUri, FtpRemoteExists.Overwrite, true);
            }
            else // SFTP
            {
                using (var fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                {
                    _sftpClient.UploadFile(fs, remoteUri, true);
                }
            }
        }

        public void UploadFiles(IEnumerable<string> localFilePaths)
        {
            foreach (var file in localFilePaths)
            {
                UploadFile(file);
            }
        }

        private string CombinePath(string dir, string file)
        {
            if (!dir.EndsWith("/")) dir += "/";
            return dir + file;
        }

        public void Dispose()
        {
            Disconnect();
            if (_ftpClient != null)
            {
                _ftpClient.Dispose();
                _ftpClient = null;
            }
            if (_sftpClient != null)
            {
                _sftpClient.Dispose();
                _sftpClient = null;
            }
        }
    }
}