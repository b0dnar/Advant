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
            Write.Logs("Старт нового кола парсингу");
            Proxy _proxy = new Proxy();
            string userAgent = GetUserAgent();
            string cookie, proxy;
            AdvantWeb _web = new AdvantWeb(userAgent);
            SendData sendData;

            try
            {
                do
                {
                    proxy = await _proxy.SearchProxy();
                }
                while (proxy == null);
                Write.Logs($"Проксі - {proxy}");

                do
                {
                    cookie = await _web.GetCookie(proxy);
                }
                while (cookie == null);

                do
                {
                    cookie = await _web.GetSessionId(cookie, proxy, dataInput.Login, dataInput.Password);
                }
                while (cookie == null);
                Write.Logs($"Кукі - {cookie}");

                sendData = new SendData(_web, cookie, proxy);

                bool stateSetCity = false;
                while(!stateSetCity)
                {
                    stateSetCity = await SetCityFrom(dataInput.NameCityFrom, sendData);
                }

                List<string> filters;
                do
                {
                    filters = await GetFilters(dataInput, sendData);
                }
                while (filters == null);

                AdvantParse parse = new AdvantParse(sendData, dataInput.NameCountryFrom);
                List<DataOutput> allData = new List<DataOutput>();

                Write.Logs($"Всього {filters.Count} фільтрів");
                
                //TODO async foreach
                var count = 1;
                foreach (var item in filters)
                {
                    var url = await _web.SendFilter(item, cookie, proxy);

                    if (url == null)
                    {
                        count++;
                        Write.Logs($"Не отримано ссилку по фільтру.\nFilter - {url}");
                        continue;
                    }

                    int countRequest = 0, maxRequest = 5;
                    bool noLoadHotels = true;
                    while (noLoadHotels)
                    {
                        if (countRequest == maxRequest)
                        {
                            break;
                        }
                        var jObj = await _web.LoadHotels(url, cookie, proxy);

                        if (jObj == null || !jObj.ToString().Contains("percent"))
                        {
                            countRequest++;
                            continue;
                        }

                        var percent = (int)jObj["percent"];
                        if (percent == 100)
                        {
                            noLoadHotels = false;
                        }
                    }

                    if (noLoadHotels)
                    {
                        count++;
                        Write.Logs("Не може загрузити готелі");
                        continue;
                    }

                    var datas = await parse.ParseHotel(url);
                    if (datas == null)
                    {
                        count++;
                        Write.Logs("Помилка при парсингу готелів");
                        continue;
                    }

                    lock (allData)
                    {
                        allData.AddRange(datas);
                    }

                    Write.Logs($"Парсинг {count} фільтрів завершено");
                    count++;
                }

                Write.Result(allData);
                Write.Logs("Файл записано!");
            }
            catch (Exception ex)
            {
                Write.Logs("Помилка в головному потоку");
                Write.Logs(ex.ToString());
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

        private async Task<bool> SetCityFrom(string nameCountryFrom, SendData sData)
        {
            try
            {
                var html = await sData.Web.GetListCity(sData.Cookie, sData.Proxy);
                var elements = html.DocumentNode.SelectNodes("//li[@class='fz16']");
                var elemUrl = elements.FirstOrDefault(x => x.InnerText.Contains(nameCountryFrom));

                if (elemUrl.ChildNodes[1].Name == "a")
                {
                    var urlCity = elemUrl.SelectSingleNode(".//a").Attributes["href"].Value;

                    await sData.Web.SetCity(urlCity, sData.Cookie, sData.Proxy);
                }

                return true;
            }
            catch (Exception e)
            {
                Write.Logs("Помилка Встановленні міста вильоту");
                Write.Logs(e.ToString());

                return false;
            }
        }

        private async Task<List<string>> GetFilters(DataInput data, SendData sData)
        {
            List<string> listFilters = new List<string>();

            try
            {
                string flagCountryFrom = data.NameCountryFrom.Equals("Украина") ? "ua" : "ru";
                int addFromStar = data.NameCountryFrom.Equals("Украина") ? 3 : 402;

                string idCountry = await GetIdCountry(flagCountryFrom, data.NameCountryTo, sData);
                if (idCountry == null)
                {
                    return null;
                }

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
                Write.Logs(log);
                return null;
            }

            return listFilters;
        }

        private async Task<string> GetIdCountry(string flagUrl, string nameCountry, SendData sData)
        {
            try
            {
                var html = await sData.Web.GetStartPage(flagUrl, sData.Cookie, sData.Proxy);
                var listCountrys = html.DocumentNode.SelectNodes("//select[@id='id_country']/option");
                var selectCountry = listCountrys.FirstOrDefault(x => x.InnerText.Contains(nameCountry));
                var rez = selectCountry.Attributes["value"].Value;

                return rez;
            }
            catch (Exception ex)
            {
                Write.Logs("Помилка при отриманні ід міста");
                Write.Logs(ex.ToString());

                return null;
            }
        }
    }
}
