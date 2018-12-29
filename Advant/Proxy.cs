using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace Advant
{
    public class Proxy
    {
        public async Task<string> SearchProxy()
        {
            string proxy = "";
            string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.111 Safari/537.36";
            AdvantWeb _web = new AdvantWeb(userAgent, null);
            
            List<string> allProxy = new List<string>();
            Random rand = new Random();

            List<string> listUrl = new List<string>();
            listUrl.Add("https://www.proxynova.com/proxy-server-list/country-ru/");
            listUrl.Add("https://www.proxynova.com/proxy-server-list/country-ua/");

            try
            {
                foreach (var item in listUrl)
                {
                    string kod = await _web.DownloadListProxy(item);
                    allProxy.AddRange(GetListProxy(kod));
                }


                allProxy = allProxy.OrderBy(c => rand.Next()).ToList();

                foreach (var item in allProxy)
                {
                    string[] proxyList = item.Split(':');

                    if (await _web.TestProxy(proxyList[0], Convert.ToInt32(proxyList[1])))
                    {
                        proxy = item;
                        break;
                    }
                }
            }
            catch
            {
                return null;
                //ConfigurationFile.WriteLog("Error in search proxy");
            }

            return proxy;
        }

        private List<string> GetListProxy(string kodPage)
        {
            List<string> lRez = new List<string>();
            HtmlDocument html = new HtmlDocument();
            Regex regIp = new Regex(@"12345678(?<val1>.*?)'.*?'(?<val2>.*?)'");
            Regex regPort = new Regex("[0-9]+");

            try
            {
                html.LoadHtml(kodPage);
                var list = html.DocumentNode.SelectNodes("//tbody/tr");

                foreach (var item in list)
                {
                    string temp = item.SelectSingleNode(".//td[1]").InnerHtml;
                    string ip = regIp.Match(temp).Groups["val1"].Value + regIp.Match(temp).Groups["val2"].Value;

                    temp = item.SelectSingleNode(".//td[2]").InnerText;
                    string port = regPort.Match(temp).Value;

                    lRez.Add(ip + ":" + port);
                }
            }
            catch { }

            return lRez;
        }

    }
}
