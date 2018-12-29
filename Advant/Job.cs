using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Advant
{
    public class Job
    {
        public delegate void WriteToFile(string str);

        public static WriteToFile SystemLogging;
        public WriteToFile WriteData;


        private AdvantWeb _web;
        private string cookie;
        private string proxy;

        public DataInput ParseInputFile()
        {
            DataInput data = new DataInput();

            try
            {
                string path = Directory.GetCurrentDirectory();

                using (var reader = new StreamReader(path + @"/config.json"))
                {
                    string str = reader.ReadToEnd();
                    var obj = JObject.Parse(str);

                    data.Login = obj["Login"].ToString();
                    data.Password = obj["Password"].ToString();
                    data.NameCountryFrom = obj["CountryFrom"].ToString();
                    data.NameCityFrom = obj["CityFrom"].ToString();
                    data.NameCountryTo = obj["CountryTo"].ToString();
                    data.TimeSleep = (int)(obj["TimeSleep"]);
                }
            }
            catch (Exception e)
            {
            }

            return data;
        }
        public async Task Run(DataInput dataInput)
        {
            SystemLogging += WriteLogs;

            Proxy _proxy = new Proxy();
            string userAgent = GetUserAgent();

            _web = new AdvantWeb(userAgent, SystemLogging);

            proxy = await _proxy.SearchProxy();
            if (proxy == null)
            {
                SystemLogging("Проксі не знайдено");
                return;
            }

            cookie = await _web.GetCookie(proxy);
            if (cookie == null)
            {
                SystemLogging("Кукі не отримано");
                return;
            }

            cookie = await _web.GetSessionId(cookie, proxy, dataInput.Login, dataInput.Password);
            if (cookie == null)
            {
                SystemLogging("Помилка при авторизації");
                return;
            }

            List<string> filters = await GetFilters(dataInput);
        }

        private string GetUserAgent()
        {
            Random random = new Random();
            int value = random.Next(1, 10);
            string agent = "";

            switch (value)
            {
                case 1:
                    agent = "Mozilla/5.0 (Linux; Android 7.0; SM-G930VC Build/NRD90M; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/58.0.3029.83 Mobile Safari/537.36";
                    break;
                case 2:
                    agent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/69.0.3497.105 Mobile/15E148 Safari/605.1";
                    break;
                case 3:
                    agent = "Mozilla/5.0 (Linux; Android 7.0; Pixel C Build/NRD90M; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/52.0.2743.98 Safari/537.36";
                    break;
                case 4:
                    agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246";
                    break;
                case 5:
                    agent = "Mozilla/5.0 (X11; CrOS x86_64 8172.45.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.64 Safari/537.36";
                    break;
                case 6:
                    agent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_2) AppleWebKit/601.3.9 (KHTML, like Gecko) Version/9.0.2 Safari/601.3.9";
                    break;
                case 7:
                    agent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.111 Safari/537.36";
                    break;
                case 8:
                    agent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.90 Safari/537.36";
                    break;
                case 9:
                    agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36";
                    break;
                case 10:
                    agent = "Mozilla/5.0 (Windows NT 5.1; rv:7.0.1) Gecko/20100101 Firefox/7.0.1";
                    break;
            }

            return agent;
        }

        

        private async Task<List<string>> GetFilters(DataInput data)
        {
            List<string> listFilters = new List<string>();

            var html = await _web.GetListCity(cookie, proxy);
            var elements = html.DocumentNode.SelectNodes("//li[@class='fz16']");
            var urlCityFrom = elements.FirstOrDefault(x => x.InnerText == data.NameCityFrom).Attributes["href"];

            return listFilters;
        }

        private void WriteLogs(string log)
        {
            string path = Directory.GetCurrentDirectory();

            using (var wr = new StreamWriter(path + @"/log.txt", true))
            {
                string data = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " -  ";
                wr.WriteLine(data + log);
            }
        }

        

    }
}
