using Microsoft.Extensions.Configuration;
using CIAPI.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CIAPI.DataManagers
{
    public class DAL
    {
        private static SqlConnection cnnDefault { get; set; }
        public static string GetMainConnStr()
        {
            String path;
            if (File.Exists("appsettings.json"))
            {
                path = "appsettings.json";
            }
            else
            {
                path = "wwwroot/appsettings.json";
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path);

            var configuration = builder.Build();
            var connectionString = configuration.GetConnectionString("MainContext");
            return connectionString;
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


            SqlConnection connection = null;

            connection = new SqlConnection(connStr);

            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {

                Console.Write(ex.ToString());
                throw;
            }

            return connection;

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
        public static DataSet GetDataSet(string queryText)
        {

            var conn = GetDefaultConnection();


            var ds = GetDataSet(conn, queryText);

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
                SqlCommand command = new SqlCommand(queryText,  conn, transaction);
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

        public static List<String> GetStringColumn(SqlConnection conn, string queryText){
           // var conn = GetDefaultConnection();

            List<String> columnData = new List<String>();

                using(SqlCommand command = new SqlCommand(queryText, conn))
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

        public static List<String> GetStringColumn(SqlConnection conn,SqlTransaction transaction, string queryText)
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



        public static DataTable GetTable(string queryText)
        {

            var conn = GetDefaultConnection();

            var table = GetTable(conn, queryText);



            return table;
        }

        public static List<T> GetTableMapped<T>(SqlConnection conn, SqlTransaction transaction, string queryText)
        {
            List<T> ret;
            if (conn != null && transaction!=null)
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
        public static List<T> GetTableMapped<T>(string queryText)
        {

            var conn = GetDefaultConnection();
            var table = GetTableMapped<T>(conn, queryText);



            return table;

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
                        


                        pro.SetValue(objT, row[pro.Name] == DBNull.Value ? null : Convert.ChangeType(row[pro.Name], pI.PropertyType));






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
