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
       // private Regex regNum = new Regex(@"[^\d]");
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
    }
}
