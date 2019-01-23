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
        private Regex regNum = new Regex(@"[^\d]");
        private string Cookie { get; set; }
        private string Proxy { get; set; }
        private AdvantWeb _web;

        public AdvantParse(AdvantWeb web, string cookie, string proxy)
        {
            _web = web;
            Cookie = cookie;
            Proxy = proxy;
        }


        public async Task ParseHotel(string urlFilter)
        {
            var htmlHotels = await _web.GetHotels(urlFilter, Cookie, Proxy);
            var urlHotels = GetUrlHotels(htmlHotels);

            List<string> urlsTour = new List<string>();

            foreach (var url in urlHotels)
            {
                var htmlTour = await _web.GetTours(url, Cookie, Proxy);
                var urls = GetUrlTours(htmlTour);
                urlsTour.AddRange(urls);
            }

            foreach (var url in urlsTour)
            {
                var htmlTour = await _web.GetPageTour(url, Cookie, Proxy);
                var data = FillingData(htmlTour);
            }











        }

        private List<string> GetUrlHotels(HtmlDocument html)
        {
            var collectHref = html.DocumentNode.SelectNodes("//div[@class='col-12 col-md-3 col-lg-4 col-xl-3 bl-list__button-hor']");
            var listUrls = collectHref.Select(x => x.SelectSingleNode(".//a").Attributes["href"].Value.Replace("#", "https://advant.club").Replace("?show_filter=1", "")).ToList();

            return listUrls;
        }

        private List<string> GetUrlTours(HtmlDocument html)
        {
            var collectDiv = html.DocumentNode.SelectNodes("//div[@class='col-12 col-xl-2 mb-sm-no-xl']");
            var list = collectDiv.Select(x => x?.SelectSingleNode("//a").Attributes["href"].Value ?? "").ToList();

            return list;
        }

        private DataOutput FillingData(HtmlDocument html)
        {
            DataOutput data = new DataOutput();

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
                        data.CountAdults = regNum.Match(value).Value;
                        break;
                    case "Питание":
                        data.FoodBig = value;
                        break;
                    case "Номер":
                        data.TypeRoomBig = value;
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
                    //data.DataFrom
                }
            }

            data.Operator = html.DocumentNode.SelectSingleNode("//li[@class='d-block d-md-none']").InnerText;

            data.Price = html.DocumentNode.SelectSingleNode("//span[@class='fz24']").InnerText;

            return data;
        }
    }
}
