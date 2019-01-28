using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;


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

            return data;
        }

        public async Task Run(DataInput dataInput)
        {
            try
            {
                SystemLogging += WriteLogs;

                SystemLogging("Старт нового кола парсингу");
                Proxy _proxy = new Proxy();
                string userAgent = GetUserAgent();

                _web = new AdvantWeb(userAgent, SystemLogging);

                proxy = await _proxy.SearchProxy();
                if (proxy == null)
                {
                    SystemLogging("Проксі не знайдено");
                    return;
                }
                SystemLogging($"Проксі - {proxy}");

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
                SystemLogging($"Кукі - {cookie}");

                await SetCityFrom(dataInput.NameCityFrom);
                List<string> filters = await GetFilters(dataInput);

                AdvantParse parse = new AdvantParse(_web, dataInput.NameCountryFrom, cookie, proxy);
                List<DataOutput> allData = new List<DataOutput>();

                SystemLogging($"Всього {filters.Count} фільтрів");
                //TODO async foreach
                var count = 1;
                foreach (var item in filters)
                {
                    string url = await _web.SendFilter(item, cookie, proxy);

                    if (url == null)
                    {
                        continue;
                    }

                    bool noLoadHotels = true;
                    while (noLoadHotels)
                    {
                        var jObj = await _web.LoadHotels(url, cookie, proxy);

                        if (jObj == null)
                        {
                            break;
                        }

                        var percent = (int)jObj["percent"];
                        if (percent == 100)
                        {
                            noLoadHotels = false;
                        }
                    }


                    var datas = await parse.ParseHotel(url);

                    lock (allData)
                    {
                        allData.AddRange(datas);
                    }

                    SystemLogging($"Парсинг {count} фільтрів завершено");
                    count++;
                }

                Save(allData);
                SystemLogging("Файл записано!");
            }
            catch
            {

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

            try
            {
                string flagCountryFrom = data.NameCountryFrom.Equals("Украина") ? "ua" : "ru";
                int addFromStar = data.NameCountryFrom.Equals("Украина") ? 3 : 402;

                string idCountry = await GetIdCountry(flagCountryFrom, data.NameCountryTo);

                string dateF = DateTime.Now.AddDays(data.StartDay).ToString("dd.MM.yyyy");
                string dateT = DateTime.Now.AddDays(data.StartDay + data.CountDay - 1).ToString("dd.MM.yyyy");

                for (var nightFrom = 6; nightFrom <= 21; nightFrom++)
                {
                    // for (var stars = 3; stars < 5; stars++)
                    // {
                    var stars1 = addFromStar;
                    var stars2 = stars1 + 1;
                    var stars3 = stars2 + 1;
                    var nightTo = nightFrom + 7;


                    listFilters.Add($"https://advant.club/{flagCountryFrom}/search/?country={idCountry}&date_from={dateF}&date_till={dateT}&night_from={nightFrom}&night_till={nightTo}&adult_amount=2&child_amount=0&child1_age=12&child2_age=1&child3_age=1&hotel_ratings={stars1}&hotel_ratings={stars2}&hotel_ratings={stars3}&meal_types=all&price_budget=0-1000000&price_from=0&price_till=100000");
                    listFilters.Add($"https://advant.club/{flagCountryFrom}/search/?country={idCountry}&date_from={dateF}&date_till={dateT}&night_from={nightFrom}&night_till={nightTo}&adult_amount=2&child_amount=1&child1_age=12&child2_age=1&child3_age=1&hotel_ratings={stars1}&hotel_ratings={stars2}&hotel_ratings={stars3}&meal_types=all&price_budget=0-1000000&price_from=0&price_till=100000");
                    listFilters.Add($"https://advant.club/{flagCountryFrom}/search/?country={idCountry}&date_from={dateF}&date_till={dateT}&night_from={nightFrom}&night_till={nightTo}&adult_amount=2&child_amount=2&child1_age=12&child2_age=1&child3_age=1&hotel_ratings={stars1}&hotel_ratings={stars2}&hotel_ratings={stars3}&meal_types=all&price_budget=0-1000000&price_from=0&price_till=100000");
                    // }
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
            string rez = "";

            try
            {
                var html = await _web.GetStartPage(flagUrl, cookie, proxy);
                var listCountrys = html.DocumentNode.SelectNodes("//select[@id='id_country']/option");
                var selectCountry = listCountrys.FirstOrDefault(x => x.InnerText.Contains(nameCountry));
                rez = selectCountry.Attributes["value"].Value;
            }
            catch
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


        public static void Save(List<DataOutput> list)
        {
            string header = "INSERT INTO `proposal` (`id`, `countryFrom`, `countryWhere`, `cityFrom`, `cityWhere`, `adults`, `children`, `hotelName`, `hotelRate`, `hotelDateFrom`, `hotelNights`, `hotelFood`, `hotelRoom`, `hotelPrice`, `operatorName`) VALUES";
            Random r = new Random();

            int count = 1;
            string path = Directory.GetCurrentDirectory();

            var time = DateTime.Now.ToString("HH_mm");
            using (StreamWriter sw = new StreamWriter($"{path}/database_{time}.sql"))
            {
                try
                {
                    sw.WriteLine(header);

                    foreach (var d in list)
                    {
                        StringBuilder str = new StringBuilder();

                        str.AppendFormat("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}'),", count, d.CountryFrom, d.CountryTo, d.CityFrom, d.CityTo, d.CountAdults, d.CountChildren, d.NameHotel, d.CountStart, d.DataFrom, d.CountNight, d.Food, d.TypeRoom, d.Price, 0, d.Operator);

                        if (count++ == list.Count)
                            str.Replace("),", ");");

                        sw.WriteLine(str.ToString());
                    }
                }
                catch
                {
                    //WriteLog("Error in Write Data to file\n" + e.ToString());
                }
            }
        }

    }
}
