using CIAPI.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CIAPI.DataManagers
{
    public class DAL
    {
        private static SqlConnection cnnDefault { get; set; }
        public static string GetMainConnStr()
        {
            try
            {
                String path = "";
                var currentDir = Directory.GetCurrentDirectory();
                var baseDir = AppContext.BaseDirectory;

                Console.WriteLine($"[DAL] CurrentDir: {currentDir}");
                Console.WriteLine($"[DAL] BaseDir: {baseDir}");

                // Intentar varias ubicaciones posibles
                string[] possiblePaths = new[]
                {
                    Path.Combine(currentDir, "appsettings.json"),
                    Path.Combine(currentDir, "appsettings.Development.json"),
                    Path.Combine(currentDir, "appsettings.Production.json"),
                    Path.Combine(baseDir, "appsettings.json"),
                    Path.Combine(baseDir, "appsettings.Development.json"),
                    Path.Combine(baseDir, "appsettings.Production.json"),
                    "appsettings.json",
                    "appsettings.Development.json",
                    "appsettings.Production.json"
                };

                Console.WriteLine("[DAL] Buscando appsettings.json en:");
                foreach (var testPath in possiblePaths)
                {
                    var exists = File.Exists(testPath);
                    Console.WriteLine($"  - {testPath}: {(exists ? "ENCONTRADO" : "NO EXISTE")}");
                    if (exists && string.IsNullOrEmpty(path))
                    {
                        path = testPath;
                    }
                }

                if (string.IsNullOrEmpty(path))
                {
                    var allFiles = Directory.GetFiles(currentDir, "*.json", SearchOption.AllDirectories);
                    Console.WriteLine($"[DAL] Archivos JSON en el directorio:");
                    foreach (var file in allFiles.Take(10))
                    {
                        Console.WriteLine($"  - {file}");
                    }

                    throw new FileNotFoundException($"No se encontró appsettings.json. CurrentDir: {currentDir}, BaseDir: {baseDir}");
                }

                Console.WriteLine($"[DAL] Usando archivo: {path}");

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(Path.GetFullPath(path)) ?? currentDir)
                    .AddJsonFile(Path.GetFileName(path), optional: false, reloadOnChange: false);

                var configuration = builder.Build();
                var connectionString = configuration.GetConnectionString("MainContext");

                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine($"[DAL] ERROR: ConnectionString 'MainContext' no encontrado en {path}");
                    throw new InvalidOperationException($"ConnectionString 'MainContext' no encontrado en {path}");
                }

                Console.WriteLine($"[DAL] ConnectionString obtenido exitosamente (longitud: {connectionString.Length})");
                return connectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DAL] ERROR CRÍTICO: {ex.Message}");
                Console.WriteLine($"[DAL] StackTrace: {ex.StackTrace}");
                throw new InvalidOperationException($"Error al obtener ConnectionString: {ex.Message}", ex);
            }
        }
        public static SqlConnection GetDefaultConnection(string connStr = "")
        {

            //connStr = connStr == "" ? GetMainConnStr() : connStr;


            SqlConnection connection = null;
            if (cnnDefault == null || cnnDefault.State == ConnectionState.Broken)
            {
                connection = GetConnection();


            }
            else
            {
                connection = cnnDefault;
                if (cnnDefault.State == ConnectionState.Closed)
                    connection.Open();

            }
            return connection;

        }
        public static SqlConnection GetConnection(string connStr = "")
        {
            connStr = connStr == "" ? GetMainConnStr() : connStr;

            // Agregar Connect Timeout extendido si no está especificado
            if (!connStr.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) &&
                !connStr.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase))
            {
                connStr = connStr.TrimEnd(';') + ";Connect Timeout=60;";
            }

            const int maxReintentos = 3;
            int[] demorасSegundos = { 2, 5, 10 }; // backoff exponencial

            SqlConnection connection = null;
            Exception ultimaExcepcion = null;

            for (int intento = 0; intento < maxReintentos; intento++)
            {
                try
                {
                    connection = new SqlConnection(connStr);
                    connection.Open();

                    if (intento > 0)
                        Console.WriteLine($"[DAL] Conexión exitosa en intento {intento + 1}");

                    return connection;
                }
                catch (SqlException sqlEx) when (EsErrorTransitorio(sqlEx))
                {
                    ultimaExcepcion = sqlEx;
                    Console.WriteLine($"[DAL] Error transitorio en intento {intento + 1}/{maxReintentos}: {sqlEx.Number} - {sqlEx.Message}");

                    connection?.Dispose();
                    connection = null;

                    if (intento < maxReintentos - 1)
                    {
                        Console.WriteLine($"[DAL] Reintentando en {demorасSegundos[intento]} segundos...");
                        System.Threading.Thread.Sleep(demorасSegundos[intento] * 1000);
                    }
                }
                catch (Exception ex)
                {
                    connection?.Dispose();
                    Console.WriteLine($"[DAL] Error no transitorio: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine($"[DAL] Agotados {maxReintentos} intentos de conexión.");
            throw new InvalidOperationException(
                $"No se pudo conectar a la base de datos luego de {maxReintentos} intentos. " +
                "La base de datos puede estar iniciando tras un período de inactividad. Por favor, intente nuevamente.",
                ultimaExcepcion);
        }

        /// <summary>
        /// Determina si un SqlException es transitorio (timeout, conexión cerrada, Azure pausado)
        /// </summary>
        private static bool EsErrorTransitorio(SqlException ex)
        {
            // Códigos de error transitorios de Azure SQL y SQL Server
            int[] errorTransitorios = {
                -2,     // Timeout de conexión
                2,      // No se encontró el servidor
                53,     // Error de red
                121,    // El semáforo agotó el tiempo de espera
                233,    // Conexión cerrada por el servidor
                258,    // Timeout de espera
                1205,   // Deadlock
                10053,  // Conexión anulada por software
                10054,  // Restablecimiento de conexión
                10060,  // Tiempo de espera de conexión agotado
                40143,  // Conexión no puede procesar la solicitud (Azure)
                40197,  // Error en el procesamiento de la solicitud (Azure)
                40501,  // Servicio ocupado (Azure throttling)
                40613,  // Base de datos no disponible (Azure)
                49918,  // No se puede procesar (Azure)
                49919,  // No se puede crear/actualizar (Azure)
                49920,  // Demasiadas operaciones (Azure)
            };

            return errorTransitorios.Contains(ex.Number) ||
                   ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("tiempo de espera", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("previa al inicio de sesión", StringComparison.OrdinalIgnoreCase);
        }
        public static void CloseDefaultConnection()
        {


            if (cnnDefault != null && cnnDefault.State == ConnectionState.Open)
            {
                cnnDefault.Close();
                cnnDefault.Dispose();
                cnnDefault = null;
            }
        }
        public static DataSet GetDataSet(SqlConnection conn, string queryText)
        {

            DataSet ds = null;
            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryText, conn);
                SqlDataAdapter adapter = new SqlDataAdapter();
                ds = new DataSet();


                adapter.SelectCommand = command;
                _ = adapter.Fill(ds);
            }
            else
            {
                ds = GetDataSet(queryText);
            }

            return ds;
        }
        public static DataSet GetDataSet(SqlConnection conn, SqlCommand cmd)
        {

            DataSet ds = null;
            if (conn != null)
            {
                //SqlCommand command = new SqlCommand(conn,cmd);
                SqlDataAdapter adapter = new SqlDataAdapter();
                ds = new DataSet();


                adapter.SelectCommand = cmd;
                _ = adapter.Fill(ds);
            }
            else
            {
                ds = GetDataSet(cmd);
            }

            return ds;
        }

        public static DataSet GetDataSet(SqlConnection conn, SqlTransaction transaction, string queryText)
        {

            DataSet ds = null;
            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryText, conn, transaction);
                SqlDataAdapter adapter = new SqlDataAdapter();
                ds = new DataSet();


                adapter.SelectCommand = command;
                _ = adapter.Fill(ds);
            }
            else
            {
                ds = GetDataSet(queryText);
            }

            return ds;
        }
        public static DataSet GetDataSet(SqlConnection conn, SqlTransaction transaction, SqlCommand cmd)
        {

            DataSet ds = null;
            if (conn != null)
            {
                //SqlCommand command = new SqlCommand(queryText, conn, transaction);
                SqlDataAdapter adapter = new SqlDataAdapter();
                ds = new DataSet();


                adapter.SelectCommand = cmd;
                _ = adapter.Fill(ds);
            }
            else
            {
                ds = GetDataSet(cmd);
            }

            return ds;
        }
        public static DataSet GetDataSet(string queryText)
        {

            var conn = GetDefaultConnection();


            var ds = GetDataSet(conn, queryText);

            //conn.Dispose();


            return ds;
        }

        public static DataSet GetDataSet(SqlCommand cmd)
        {

            var conn = GetDefaultConnection();


            var ds = GetDataSet(conn, cmd);

            //conn.Dispose();


            return ds;
        }

        public static int GetScalar(SqlConnection conn, string queryText)
        {

            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryText, conn);
                SqlDataAdapter adapter = new SqlDataAdapter();
                var result = command.ExecuteScalar();


                if (result != null)
                {
                    int final;
                    if (int.TryParse(result.ToString(), out final))
                    {
                        return final;
                    }
                }
            }
            else
            {

                return GetScalar(queryText);
            }
            return 0;
        }
        public static int GetScalar(SqlConnection conn, SqlTransaction transaction, string queryText)
        {

            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryText, conn, transaction);
                SqlDataAdapter adapter = new SqlDataAdapter();
                var result = command.ExecuteScalar();


                if (result != null)
                {
                    int final;
                    if (int.TryParse(result.ToString(), out final))
                    {
                        return final;
                    }
                }
            }
            else
            {

                return GetScalar(queryText);
            }
            return 0;
        }

        public static int GetScalar(string queryText)
        {
            var conn = GetDefaultConnection();
            return GetScalar(conn, queryText);
        }

        public static List<String> GetStringColumn(SqlConnection conn, string queryText)
        {
            // var conn = GetDefaultConnection();

            List<String> columnData = new List<String>();

            using (SqlCommand command = new SqlCommand(queryText, conn))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columnData.Add(reader.GetString(0));
                    }
                }
            }

            return columnData;

        }

        #region "GetDouble"
        public static double GetDouble(SqlConnection conn, string queryText)
        {

            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryText, conn);
                SqlDataAdapter adapter = new SqlDataAdapter();
                var result = command.ExecuteScalar();


                if (result != null)
                {
                    double final;
                    if (double.TryParse(result.ToString(), out final))
                    {
                        return final;
                    }
                }
            }
            else
            {

                return GetDouble(queryText);
            }
            return 0;
        }
        public static double GetDouble(SqlConnection conn, SqlTransaction transaction, string queryText)
        {

            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryText, conn, transaction);
                SqlDataAdapter adapter = new SqlDataAdapter();
                var result = command.ExecuteScalar();


                if (result != null)
                {
                    double final;
                    if (double.TryParse(result.ToString(), out final))
                    {
                        return final;
                    }
                }
            }
            else
            {

                return GetDouble(queryText);
            }
            return 0;
        }

        public static double GetDouble(string queryText)
        {
            var conn = GetDefaultConnection();
            return GetDouble(conn, queryText);
        }



        #endregion

        public static List<String> GetStringColumn(SqlConnection conn, SqlTransaction transaction, string queryText)
        {
            // var conn = GetDefaultConnection();

            List<String> columnData = new List<String>();

            using (SqlCommand command = new SqlCommand(queryText, conn, transaction))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columnData.Add(reader.GetString(0));
                    }
                }
            }

            return columnData;

        }

        public static DateTime GetDateTime(SqlConnection conn, string queryText)
        {
            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryText, conn);
                SqlDataAdapter adapter = new SqlDataAdapter();
                var result = command.ExecuteScalar();


                if (result != null)
                {
                    DateTime final;
                    if (DateTime.TryParse(result.ToString(), out final))
                    {
                        return final;
                    }
                }
            }
            else
            {
                return GetDateTime(queryText);
            }
            return new DateTime(1900, 1, 1);
        }

        public static DateTime GetDateTime(string queryText)
        {
            var conn = GetDefaultConnection();
            return GetDateTime(conn, queryText);
        }
        public static int Execute(SqlConnection conn, string queryInsUpd)
        {
            int ret = 0;
            if (conn != null)
            {
                SqlCommand command = new SqlCommand(queryInsUpd, conn);
                SqlDataAdapter adapter = new SqlDataAdapter();


                ret = command.ExecuteNonQuery();
            }
            else
            {
                ret = Execute(queryInsUpd);
            }


            return ret;
        }

        public static int Execute(string queryInsUpd)
        {
            var conn = GetDefaultConnection();


            var ret = Execute(conn, queryInsUpd);



            return ret;
        }


        public static DataTable GetTable(SqlConnection conn, SqlTransaction transaction, string queryText)
        {

            if (conn != null)
            {
                var ds = GetDataSet(conn, transaction, queryText);
                return ds.Tables[0];
            }
            else
            {
                var table = GetTable(queryText);
                return table;
            }

        }

        public static DataTable GetTable(SqlConnection conn, SqlTransaction transaction, SqlCommand cmd)
        {

            if (conn != null)
            {
                var ds = GetDataSet(conn, transaction, cmd);
                return ds.Tables[0];
            }
            else
            {
                var table = GetTable(cmd);
                return table;
            }

        }

        public static DataTable GetTable(SqlConnection conn, string queryText)
        {

            if (conn != null)
            {
                var ds = GetDataSet(conn, queryText);
                return ds.Tables[0];
            }
            else
            {
                var table = GetTable(queryText);
                return table;
            }

        }
        public static DataTable GetTable(SqlConnection conn, SqlCommand cmd)
        {

            if (conn != null)
            {
                var ds = GetDataSet(conn, cmd);
                return ds.Tables[0];
            }
            else
            {
                var table = GetTable(cmd);
                return table;
            }

        }


        public static DataTable GetTable(string queryText)
        {

            var conn = GetDefaultConnection();

            var table = GetTable(conn, queryText);



            return table;
        }


        public static DataTable GetTable(SqlCommand cmd)
        {

            var conn = GetDefaultConnection();

            var table = GetTable(conn, cmd);



            return table;
        }

        public static List<T> GetTableMapped<T>(SqlConnection conn, SqlTransaction transaction, string queryText)
        {
            List<T> ret;
            if (conn != null && transaction != null)
            {

                var table = GetTable(conn, transaction, queryText);
                ret = ConvertToList<T>(table);
            }
            else
            {
                ret = GetTableMapped<T>(queryText);
            }
            return ret;
        }

        public static List<T> GetTableMapped<T>(SqlConnection conn, SqlTransaction transaction, SqlCommand cmd)
        {
            List<T> ret;
            if (conn != null && transaction != null)
            {

                var table = GetTable(conn, transaction, cmd);
                ret = ConvertToList<T>(table);
            }
            else
            {
                ret = GetTableMapped<T>(cmd);
            }
            return ret;
        }

        public static List<T> GetTableMapped<T>(SqlConnection conn, string queryText)
        {
            List<T> ret;
            if (conn != null)
            {
                var table = GetTable(conn, queryText);
                ret = ConvertToList<T>(table);
            }
            else
            {
                ret = GetTableMapped<T>(queryText);
            }
            return ret;
        }

        public static List<T> GetTableMapped<T>(SqlConnection conn, SqlCommand cmd)
        {
            List<T> ret;
            if (conn != null)
            {
                var table = GetTable(conn, cmd);
                ret = ConvertToList<T>(table);
            }
            else
            {
                ret = GetTableMapped<T>(cmd);
            }
            return ret;
        }
        public static List<T> GetTableMapped<T>(string queryText)
        {

            var conn = GetDefaultConnection();
            var table = GetTableMapped<T>(conn, queryText);



            return table;

        }

        public static List<T> GetTableMapped<T>(SqlCommand cmd)
        {

            var conn = GetDefaultConnection();
            var table = GetTableMapped<T>(conn, cmd);



            return table;

        }



        public static double GetDouble(SqlCommand cmd)
        {
            var result = cmd.ExecuteScalar();
            return result != null && double.TryParse(result.ToString(), out double d) ? d : 0;
        }

        public static List<string> GetStringColumn(SqlCommand cmd)
        {
            var list = new List<string>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(reader.GetString(0));
            return list;
        }

        public static int Execute(SqlCommand cmd)
        {
            return cmd.ExecuteNonQuery();
        }

        public static Int32 Execute(string commandText, SqlConnection conn = null)
        {


            var eliminar = conn == null;
            conn = conn == null ? GetDefaultConnection() : conn;

            SqlCommand command = new SqlCommand(commandText, conn);

            try
            {
                //conn.Open();
                Int32 rowsAffected = command.ExecuteNonQuery();
                Console.WriteLine("RowsAffected: {0}", rowsAffected);
                return rowsAffected;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
            finally
            {
                if (eliminar) conn.Dispose();
            }

        }


        public static List<T> ConvertToList<T>(DataTable dt)
        {
            var columnNames = dt.Columns.Cast<DataColumn>()
                    .Select(c => c.ColumnName)
                    .ToList();
            var properties = typeof(T).GetProperties();
            return dt.AsEnumerable().Select(row =>
            {
                var objT = Activator.CreateInstance<T>();
                foreach (var pro in properties)
                {
                    if (columnNames.Contains(pro.Name))
                    {
                        PropertyInfo pI = objT.GetType().GetProperty(pro.Name);


                        if (row[pro.Name] == null || row[pro.Name] == DBNull.Value)
                        {
                            pro.SetValue(objT, null);
                        }
                        else
                        {
                            Type underlyingType = Nullable.GetUnderlyingType(pI.PropertyType) ?? pI.PropertyType;
                            object convertedValue = Convert.ChangeType(row[pro.Name], underlyingType);
                            pro.SetValue(objT, convertedValue);
                        }





                    }
                }
                return objT;
            }).ToList();
        }

        public static List<DataListViewModel> ConvertToDataList(DataTable dt, string idTable, string nameTable)
        {



            List<DataListViewModel> ret = new List<DataListViewModel>();
            ret = (from DataRow dr in dt.Rows
                   select new DataListViewModel()
                   {
                       Id = int.Parse(dr[idTable].ToString()),
                       Nombre = dr[nameTable].ToString(),
                       Detalle = ""
                   }).ToList();
            return ret;


        }

        public static List<DataListIdStringViewModel> ConvertToDataListStr(DataTable dt, string idTable, string nameTable)
        {



            List<DataListIdStringViewModel> ret = new List<DataListIdStringViewModel>();
            ret = (from DataRow dr in dt.Rows
                   select new DataListIdStringViewModel()
                   {
                       Id = dr[idTable].ToString(),
                       Nombre = dr[nameTable].ToString(),
                   }).ToList();
            return ret;


        }
    }
}
