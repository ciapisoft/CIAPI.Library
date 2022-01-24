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

namespace CIAPI.DataManagers
{
    public class DAL
    {
        public static string GetMainConnStr()
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("wwwroot/appsettings.json");

            var configuration = builder.Build();
            var connectionString = configuration.GetConnectionString("MainContext");
            return connectionString;
        }
        public static SqlConnection GetConnection(string connStr = "")
        {

            connStr = connStr == "" ? GetMainConnStr() : connStr;


            var connection = new SqlConnection(connStr);

            connection.Open();

            return connection;

        }

        public static DataSet GetDataSet(SqlConnection conn, string queryText)
        {
            SqlCommand command = new SqlCommand(queryText, conn);
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataSet ds = new DataSet();

            
            adapter.SelectCommand = command;
            _ = adapter.Fill(ds);


            return ds;
        }
        public static int Execute(SqlConnection conn, string queryInsUpd)
        {
            SqlCommand command = new SqlCommand(queryInsUpd, conn);
            SqlDataAdapter adapter = new SqlDataAdapter();


            var ret = command.ExecuteNonQuery();


            return ret;
        }

        public static int Execute(string queryInsUpd)
        {
            var conn = GetConnection();


            var ret = Execute(conn, queryInsUpd);

            conn.Dispose();

            return ret;
        }

        public static DataSet GetDataSet(string queryText)
        {

            var conn = GetConnection();


            var ds = GetDataSet(conn, queryText);

            conn.Dispose();

            return ds;
        }
        public static DataTable GetTable(SqlConnection conn, string queryText)
        {

            DataSet ds = GetDataSet(conn, queryText);






            return ds.Tables[0];
        }



        public static DataTable GetTable(string queryText)
        {

            var conn = GetConnection();

            var table = GetTable(conn, queryText);

            conn.Dispose();

            return table;
        }


        public static List<T> GetTableMapped<T>(SqlConnection conn, string queryText)
        {
            var table = GetTable(conn, queryText);
            var ret = ConvertToList<T>(table);
            return ret;
        }
        public static List<T> GetTableMapped<T>(string queryText)
        {

            var conn = GetConnection();
            var table = GetTableMapped<T>(conn,queryText);

            conn.Dispose();

            return table;

        }





        public static Int32 Execute(string commandText, SqlConnection conn = null)
        {


            var eliminar = conn == null;
            conn = conn == null ? GetConnection() : conn;

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

        public static List<DataListViewModel> ConvertToDataList(DataTable dt, string idTable, string nameTable )
        {



            List<DataListViewModel> ret = new List<DataListViewModel>();
            ret = (from DataRow dr in dt.Rows
                           select new DataListViewModel()
                           {
                               Id = int.Parse(dr[idTable].ToString()),
                               Nombre= dr[nameTable].ToString(),
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
