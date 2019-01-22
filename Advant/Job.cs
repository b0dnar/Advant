using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

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
                    data.StartDay = (int)obj["StartDay"];
                    data.CountDay = (int)obj["CountDay"];
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
            AdvantParse parse = new AdvantParse();
            string userAgent = GetUserAgent();

            _web = new AdvantWeb(userAgent, SystemLogging);

            proxy = "";
//            proxy = await _proxy.SearchProxy();
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

            await SetCityFrom(dataInput.NameCityFrom);
            

            List<string> filters = await GetFilters(dataInput);

            //TODO async foreach
            foreach (var item in filters)
            {
                string url = await _web.SendFilter(item, cookie, proxy);

                bool noLoadHotels = true;
                while (noLoadHotels)
                {
                    var jObj = await _web.LoadHotels(url, cookie, proxy);
                    var percent = (int) jObj["percent"];
                    if (percent == 100)
                    {
                        noLoadHotels = false;
                    }
                }

                var htmlHotels = await _web.GetHotels(url, cookie, proxy);

            }
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




        private async Task SetCityFrom(string nameCountryFrom)
        {
            var html = await _web.GetListCity(cookie, proxy);
            var elements = html.DocumentNode.SelectNodes("//li[@class='fz16']");
            var elemUrl = elements.FirstOrDefault(x => x.InnerText.Contains(nameCountryFrom));

            if (elemUrl.ChildNodes[1].Name == "a")
            {
                var urlCity = elemUrl.SelectSingleNode(".//a").Attributes["href"].Value;

                await _web.SetCity(urlCity, cookie, proxy);
            }
        }


        private async Task<List<string>> GetFilters(DataInput data)
        {
            List<string> listFilters = new List<string>();

          //  string field1 = "&meal_types=all&price_from=0&price_till=100000";//price_budget=0-100000
            //string field2 = "&";
             //string flagCountry = "ru";

            try
            {
                // if (data.NameCountryFrom.Equals("Украина"))
                //    flagCountry = "ua";

                string flagCountryFrom = data.NameCountryFrom.Equals("Украина") ? "ua" : "ru";
                int addFromStar = data.NameCountryFrom.Equals("Украина") ? 0 : 399;

                string idCountry = await GetIdCountry(flagCountryFrom, data.NameCountryTo);

                string dateF = DateTime.Now.AddDays(data.StartDay).ToString("dd.MM.yyyy");
                string dateT = DateTime.Now.AddDays(data.StartDay + data.CountDay - 1).ToString("dd.MM.yyyy");

                

                for (var nightCount = 6; nightCount <= 28; nightCount++)
                {
                    for (var stars = 3; stars <= 5; stars++)
                    {
                        addFromStar += stars;
                      //   if (data.NameCountryFrom.Equals("Украина"))
                      //  {           
                        listFilters.Add($"https://advant.club/{flagCountryFrom}/search/?country={idCountry}&date_from={dateF}&date_till={dateT}&night_from={nightCount}&night_till={nightCount}&adult_amount=2&child_amount=0&child1_age=12&child2_age=1&child3_age=1&hotel_ratings={addFromStar}&meal_types=all&price_budget=0-1000000&price_from=0&price_till=100000");
                                              
                        //    listFilters.Add("https://advant.club/search/?country=" + idCountry + "&date_from=" + dateF + "&date_till=" + dateT + "&night_from=" + nightCount + "&night_till=" + nightCount + "&adult_amount=2&child_amount=1&child1_age=12&child2_age=0&child3_age=0&hotel_ratings=" + stars + fieldPrice);
                        //   listFilters.Add("https://advant.club/search/?country=" + idCountry + "&date_from=" + dateF + "&date_till=" + dateT + "&night_from=" + nightCount + "&night_till=" + nightCount + "&adult_amount=2&child_amount=1&child1_age=2&child2_age=0&child3_age=0&hotel_ratings=" + stars + fieldPrice);
                       // }
                        //else
                        //{
                        //   // int tempStars = stars + 399;
                        //    listFilters.Add("https://advant.club/ru/search/?country=" + idCountry + "&date_from=" + dateF + "&date_till=" + dateT + "&night_from=" + nightCount + "&night_till=" + nightCount + "&adult_amount=2&child_amount=0&child1_age=0&child2_age=0&child3_age=0&hotel_ratings=" + tempStars);
                        //    listFilters.Add("https://advant.club/ru/search/?country=" + idCountry + "&date_from=" + dateF + "&date_till=" + dateT + "&night_from=" + nightCount + "&night_till=" + nightCount + "&adult_amount=2&child_amount=1&child1_age=12&child2_age=0&child3_age=0&hotel_ratings=" + tempStars);
                        //    listFilters.Add("https://advant.club/ru/search/?country=" + idCountry + "&date_from=" + dateF + "&date_till=" + dateT + "&night_from=" + nightCount + "&night_till=" + nightCount + "&adult_amount=2&child_amount=1&child1_age=2&child2_age=0&child3_age=0&hotel_ratings=" + tempStars);
                        //}
                    }
                }
            }
            catch (Exception e)
            {
                string log = String.Format("Error!!!\nFilters is not create.{0}", e.ToString());
                WriteLogs(log);
                return null;
            }

            return listFilters;
        }

        private async Task<string> GetIdCountry(string flagUrl, string nameCountry)
        {
            string  rez = "";

            try
            {
                var html= await _web.GetStartPage(flagUrl, cookie, proxy);
                var listCountrys = html.DocumentNode.SelectNodes("//select[@id='id_country']/option");
                var selectCountry = listCountrys.FirstOrDefault(x => x.InnerText.Contains(nameCountry));
                rez = selectCountry.Attributes["value"].Value;
            }
            catch (Exception e)
            {
               // ConfigurationFile.WriteLog("Error in search id country for name\n" + e.ToString());
            }

            return rez;
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
