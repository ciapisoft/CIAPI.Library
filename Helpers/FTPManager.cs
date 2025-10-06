using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
#if SFTP
using Renci.SshNet;
#endif

namespace CIAPI.Library.Helpers
{
    /// <summary>
    /// Clase para gestionar operaciones FTP y SFTP (por defecto FTP).
    /// Permite conectarse, cambiar de carpeta, listar, descargar y subir archivos.
    /// </summary>
    /// <example>
    /// // FTP
    /// var ftp = new FTPManager("ftp://ftp.servidor.com", "usuario", "clave");
    /// ftp.ChangeDirectory("/carpeta");
    /// var archivos = ftp.ListDirectory();
    /// ftp.DownloadFile("archivo.txt", @"C:\local\archivo.txt");
    /// ftp.UploadFile(@"C:\local\subir.txt");
    /// 
    /// // SFTP (requiere Renci.SshNet)
    /// var sftp = new FTPManager("sftp.servidor.com", "usuario", "clave", FTPManager.FtpMode.SFTP);
    /// sftp.Connect();
    /// sftp.ChangeDirectory("/remoto");
    /// var archivosSftp = sftp.ListDirectory();
    /// sftp.DownloadFile("remoto.txt", @"C:\local\remoto.txt");
    /// sftp.UploadFile(@"C:\local\subir.txt");
    /// sftp.Disconnect();
    /// </example>
    public class FTPManager : IDisposable
    {
        public enum FtpMode { FTP, SFTP }

        private readonly string _server;
        private readonly string _user;
        private readonly string _password;
        private readonly FtpMode _mode;
        private string _currentDirectory = "/";
#if SFTP
        private SftpClient _sftpClient;
#endif

        /// <summary>
        /// Inicializa el gestor FTP/SFTP.
        /// </summary>
        /// <param name="server">Servidor (ej: "ftp://ftp.servidor.com" o "sftp.servidor.com")</param>
        /// <param name="user">Usuario</param>
        /// <param name="password">Clave</param>
        /// <param name="mode">Modo de conexión (por defecto FTP)</param>
        public FTPManager(string server, string user, string password, FtpMode mode = FtpMode.FTP)
        {
            _server = server.TrimEnd('/');
            _user = user;
            _password = password;
            _mode = mode;
        }

        /// <summary>
        /// Conecta al servidor SFTP. No es necesario para FTP.
        /// </summary>
        public void Connect()
        {
            if (_mode == FtpMode.SFTP)
            {
#if SFTP
                if (_sftpClient == null)
                    _sftpClient = new SftpClient(_server, _user, _password);
                if (!_sftpClient.IsConnected)
                    _sftpClient.Connect();
#else
                throw new NotSupportedException("SFTP requiere la librería SSH.NET (Renci.SshNet).");
#endif
            }
            // Para FTP puro no se requiere conexión persistente
        }

        /// <summary>
        /// Desconecta del servidor SFTP. No es necesario para FTP.
        /// </summary>
        public void Disconnect()
        {
#if SFTP
            if (_mode == FtpMode.SFTP && _sftpClient != null && _sftpClient.IsConnected)
                _sftpClient.Disconnect();
#endif
        }

        /// <summary>
        /// Cambia la carpeta remota activa.
        /// </summary>
        /// <param name="remotePath">Ruta remota (ej: "/carpeta")</param>
        public void ChangeDirectory(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("El path remoto no puede ser vacío.");
            _currentDirectory = remotePath.StartsWith("/") ? remotePath : "/" + remotePath;
        }

        /// <summary>
        /// Lista archivos y carpetas de la carpeta remota actual.
        /// </summary>
        /// <returns>Lista de nombres de archivos y carpetas</returns>
        public List<string> ListDirectory()
        {
            if (_mode == FtpMode.FTP)
            {
                var request = CreateFtpRequest(_currentDirectory, WebRequestMethods.Ftp.ListDirectory);
                var result = new List<string>();
                using (var response = (FtpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                        result.Add(reader.ReadLine());
                }
                return result;
            }
#if SFTP
            else
            {
                Connect();
                var files = _sftpClient.ListDirectory(_currentDirectory);
                var result = new List<string>();
                foreach (var f in files)
                    result.Add(f.Name);
                return result;
            }
#else
            else
            {
                throw new NotSupportedException("SFTP requiere la librería SSH.NET (Renci.SshNet).");
            }
#endif
        }

        /// <summary>
        /// Descarga un archivo remoto a una ruta local.
        /// </summary>
        /// <param name="remoteFileName">Nombre del archivo remoto</param>
        /// <param name="localPath">Ruta local destino</param>
        public void DownloadFile(string remoteFileName, string localPath)
        {
            if (_mode == FtpMode.FTP)
            {
                var remoteUri = CombinePath(_currentDirectory, remoteFileName);
                var request = CreateFtpRequest(remoteUri, WebRequestMethods.Ftp.DownloadFile);
                using (var response = (FtpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var fs = new FileStream(localPath, FileMode.Create))
                {
                    stream.CopyTo(fs);
                }
            }
#if SFTP
            else
            {
                Connect();
                var remoteUri = CombinePath(_currentDirectory, remoteFileName);
                using (var fs = new FileStream(localPath, FileMode.Create))
                {
                    _sftpClient.DownloadFile(remoteUri, fs);
                }
            }
#endif
        }

        /// <summary>
        /// Descarga varios archivos remotos a una carpeta local.
        /// </summary>
        /// <param name="remoteFileNames">Lista de nombres de archivos remotos</param>
        /// <param name="localFolder">Carpeta local destino</param>
        public void DownloadFiles(IEnumerable<string> remoteFileNames, string localFolder)
        {
            foreach (var file in remoteFileNames)
            {
                var localPath = Path.Combine(localFolder, file);
                DownloadFile(file, localPath);
            }
        }

        /// <summary>
        /// Sube un archivo local a la carpeta remota actual.
        /// </summary>
        /// <param name="localFilePath">Ruta local del archivo</param>
        /// <param name="remoteFileName">Nombre remoto (opcional, por defecto usa el nombre local)</param>
        public void UploadFile(string localFilePath, string remoteFileName = null)
        {
            var fileName = remoteFileName ?? Path.GetFileName(localFilePath);
            var remoteUri = CombinePath(_currentDirectory, fileName);

            if (_mode == FtpMode.FTP)
            {
                var request = CreateFtpRequest(remoteUri, WebRequestMethods.Ftp.UploadFile);
                using (var fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                using (var reqStream = request.GetRequestStream())
                {
                    fs.CopyTo(reqStream);
                }
            }
#if SFTP
            else
            {
                Connect();
                using (var fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                {
                    _sftpClient.UploadFile(fs, remoteUri, true);
                }
            }
#endif
        }

        /// <summary>
        /// Sube varios archivos locales a la carpeta remota actual.
        /// </summary>
        /// <param name="localFilePaths">Lista de rutas locales</param>
        public void UploadFiles(IEnumerable<string> localFilePaths)
        {
            foreach (var file in localFilePaths)
            {
                UploadFile(file);
            }
        }

        /// <summary>
        /// Crea un request FTP.
        /// </summary>
        private FtpWebRequest CreateFtpRequest(string path, string method)
        {
            var uri = _server + path;
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = method;
            request.Credentials = new NetworkCredential(_user, _password);
            request.UseBinary = true;
            request.KeepAlive = false;
            return request;
        }

        /// <summary>
        /// Combina carpeta y archivo en una ruta remota.
        /// </summary>
        private string CombinePath(string dir, string file)
        {
            if (!dir.EndsWith("/")) dir += "/";
            return dir + file;
        }

        /// <summary>
        /// Libera recursos SFTP.
        /// </summary>
        public void Dispose()
        {
            Disconnect();
#if SFTP
            if (_sftpClient != null)
            {
                _sftpClient.Dispose();
                _sftpClient = null;
            }
#endif
        }
    }
}
