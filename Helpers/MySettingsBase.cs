using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Json;
namespace CIAPI.Helpers
{
    public static class MySettingsBase
    {
 
        public static string EMPRESA_NOMBRE
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("EMPRESA_NOMBRE");
            }
        }
        public static string API_KEY_SERVICIOS
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("API_KEY_SERVICIOS");
            }
        }
        /// <summary>
        /// Para los reportes
        /// </summary>
        public static string URL_BASE_SERVICIOS
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("URL_BASE_SERVICIOS");
            }
        }

        /// <summary>
        /// Para ingreso con cualquier usuario
        /// </summary>
        public static string GLOBAL_PASS
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("GLOBAL_P");
            }
        }
        public static string PATH_UPLOAD
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("PATH_UPLOAD");
            }
        }
        public static string URL_UPLOAD
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("URL_UPLOAD");
            }
        }

        public static string CORREO_NOMBRE_ORIGEN
        {
            get
            {

                return Conf.GetSection("MySettings").GetValue<string>("CORREO_NOMBRE_ORIGEN");
            }
        }

        public static string CORREO_SEND_MAIL
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("CORREO_SEND_MAIL");
            }
        }
        public static string PWD_CORREO
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("PWD_CORREO");
            }
        }

        public static string SERVIDOR_SMTP
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<string>("SERVIDOR_SMTP");
            }
        }
        public static int PUERTO_SMTP
        {
            get
            {
                return Conf.GetSection("MySettings").GetValue<int>("PUERTO_SMTP");
            }
        }

        //public static int IDEMPRESA_WSAFIP
        //{
        //    get
        //    {
        //        return Conf.GetSection("MySettings").GetValue<int>("IDEMPRESA_WSAFIP");
        //    }
        //}

    

 

        //public static string URL_WSAFIP
        //{
        //    get
        //    {
        //        return Conf.GetSection("MySettings").GetValue<string>("URL_WSAFIP");
        //    }
        //}

        private static IConfigurationRoot _conf;
        public static IConfigurationRoot Conf
        {
            get
            {
                if (_conf == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json");

                    _conf = builder.Build();
                }
                return _conf;
            }
        }

        public static void AddOrUpdateAppSetting<T>(string key, T value)
        {
            try
            {

                var filePath = Path.Combine(AppContext.BaseDirectory, "appSettings.json");
                string json = File.ReadAllText(filePath);
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                var sectionPath = key.Split(":")[0];
                if (!string.IsNullOrEmpty(sectionPath))
                {
                    var keyPath = key.Split(":")[1];
                    jsonObj[sectionPath][keyPath] = value;
                }
                else
                {
                    jsonObj[sectionPath] = value; // if no sectionpath just set the value
                }
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, output);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing app settings "+ex.Message);
            }
        }

    }
}
