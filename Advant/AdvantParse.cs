using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Advant
{
    public class AdvantParse
    {
        private Regex regNum = new Regex(@"\d+");
        private AdvantWeb _web;
        private string CountryFrom { get; set; }
        private string Cookie { get; set; }
        private string Proxy { get; set; }
        

        public AdvantParse(SendData sData, string nameCountryFrom)
        {
            _web = sData.Web;
            Cookie = sData.Cookie;
            Proxy = sData.Proxy;
            CountryFrom = nameCountryFrom;
        }


        public async Task<List<DataOutput>> ParseHotel(string urlFilter)
        {
            List<DataOutput> datasTours = new List<DataOutput>();
            List<string> urlsTour = new List<string>();

            try
            {
                var htmlHotels = await _web.GetHotels(urlFilter, Cookie, Proxy);
                if (htmlHotels == null)
                {
                    Write.Logs("Не отримано дані готелів");
                    return null;
                }

                var urlHotels = GetUrlHotels(htmlHotels);

                if (urlHotels == null)
                {
                    Write.Logs("Не має ссилок на готелі");
                    return null;
                }

                foreach (var url in urlHotels)
                {
                    var htmlTour = await _web.GetTours(url, Cookie, Proxy);
                    if (htmlTour == null)
                    {
                        Write.Logs("Не отримано дані по туру");
                        continue;
                    }

                    var urls = GetUrlTours(htmlTour);
                    if (htmlTour == null)
                    {
                        Write.Logs("Не має ссилок на тури");
                        continue;
                    }

                    urlsTour.AddRange(urls);
                }

                foreach (var url in urlsTour)
                {
                    var htmlTour = await _web.GetPageTour(url, Cookie, Proxy);
                    if (htmlTour == null)
                    {
                        Write.Logs("Не отримано тур");
                        continue;
                    }

                    var data = FillingData(htmlTour, url);
                    if (data != null)
                    {
                        data.UrlTour = $"https://advant.club{url}";
                        data.CountryFrom = CountryFrom;

                        datasTours.Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Write.Logs("Помилка при парсингу даних готелів");
                Write.Logs(ex.ToString());
            }

            return datasTours;
        }

        private List<string> GetUrlHotels(HtmlDocument html)
        {
            try
            {
                var collectHref = html.DocumentNode.SelectNodes("//div[@class='col-12 col-md-3 col-lg-4 col-xl-3 bl-list__button-hor']");
                var listUrls = collectHref.Select(x => x.SelectSingleNode(".//a").Attributes["href"].Value.Replace("#", "https://advant.club").Replace("?show_filter=1", "")).ToList();

                return listUrls;
            }
            catch (Exception ex)
            {
                Write.Logs("Помилка при отриманні URL готелю");
                Write.Logs(ex.ToString());
                return null;
            }
           
        }

        private List<string> GetUrlTours(HtmlDocument html)
        {
            try
            {
                var collect = html.DocumentNode.SelectNodes("//a[@target='_blank']");
                var list = collect.Select(x => x.Attributes["href"].Value).ToList();

                return list;
            }
            catch(Exception ex)
            {
                Write.Logs("Помилка при отриманні URL туру");
                Write.Logs(ex.ToString());
                return null;
            }
        }

        private DataOutput FillingData(HtmlDocument html, string r)
        {
            if (!html.Text.Contains("h2 mb0 pb10"))
            {
                return null;
            }
            DataOutput data = new DataOutput();

            try
            {
                data.NameHotel = html.DocumentNode.SelectSingleNode("//h1[@class='h2 mb0 pb10']").InnerText;

                var star = html.DocumentNode.SelectSingleNode("//div[@class='bl-list-stars pull-left']/i").Attributes["class"].Value;
                data.CountStart = regNum.Match(star).Value;

                var names = html.DocumentNode.SelectSingleNode("//p[@class='fz18 mb10']").InnerText.Replace("\n", "").Replace("\t", "");
                var arrName = names.Split(',');
                data.CountryTo = arrName[1];
                data.CityTo = arrName[0];

                var collTagMB20 = html.DocumentNode.SelectNodes("//li[@class='mb20']");
                foreach (var tag in collTagMB20)
                {
                    var type = tag.SelectSingleNode(".//span").InnerText;
                    var value = tag.SelectSingleNode(".//p").InnerText;

                    switch (type)
                    {
                        case "Туристы":
                            var turists = value.Split('+');
                            data.CountAdults = regNum.Match(turists[0]).Value;

                            if (turists.Length == 2)
                            {
                                data.CountChildren = regNum.Match(turists[1]).Value; ;
                            }
                            else
                            {
                                data.CountChildren = "0";
                            }
                            break;
                        case "Питание":
                            data.Food = value;
                            break;
                        case "Номер":
                            data.TypeRoom = value;
                            break;
                        case "Ночей в туре":
                            data.CountNight = regNum.Match(value).Value;
                            break;
                    }
                }

                var collTagMBDown = html.DocumentNode.SelectNodes("//li[@class='mb-down-md-20']");
                foreach (var tag in collTagMBDown)
                {
                    var type = tag.SelectSingleNode(".//span").InnerText;
                    var value = tag.SelectSingleNode(".//p").InnerText;

                    if (type.Equals("Город вылета"))
                    {
                        data.CityFrom = value;
                    }
                    else if (type.Equals("Дата вылета"))
                    {
                        data.DataFrom = value.Split(',')[0];
                    }
                }

                data.Operator = html.DocumentNode.SelectSingleNode("//li[@class='d-block d-md-none']/p").InnerText;

                data.Price = html.DocumentNode.SelectSingleNode("//span[@class='fz24']").InnerText;

                return data;
            }
            catch(Exception ex)
            {
                Write.Logs("Помилка при парсингу готеля");
                Write.Logs(ex.ToString());
                return null;
            }
        }
    }
}
