using System;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Advant
{
    public class AdvantWeb
    {
        private string UserAgent { set; get; }
        private string AcceptLanguage { set; get; }
        private string AcceptEncoding { get; set; }
        private string ContentType { get; set; }
        private Job.WriteToFile Log;

        Regex regTokenVal = new Regex(@"csrftoken=(?<val>.*?);");
        Regex regSessId = new Regex(@"sessionid=.*?;");

        public AdvantWeb(string agent, Job.WriteToFile logging)
        {
            UserAgent = agent;
            AcceptLanguage = "uk-UA,uk;q=0.9,ru;q=0.8,en-US;q=0.7,en;q=0.6";
            AcceptEncoding = "gzip, deflate, br";
            ContentType = "application/x-www-form-urlencoded";
            Log = logging;
        }

        public async Task<string> GetCookie(string proxy)
        {

            var time = GetTimeRequest();

            var request = (HttpWebRequest)WebRequest.Create("https://advant.club/auth/login/");

            request.Method = "GET";
            request.ContentType = ContentType;
            request.UserAgent = UserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.Referer = "https://advant.club/";
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //    request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();
                var resCookie = response.Headers["Set-cookie"];

                return resCookie;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        public async Task<string> GetSessionId(string cookie, string proxy, string login, string pass)
        {
            string cookiesPage = "";
            var token = regTokenVal.Match(cookie).Groups["val"].Value;
            string content = $"next=&csrfmiddlewaretoken={token}&login_or_email={login}&password={pass}";

            var request = (HttpWebRequest)WebRequest.Create("https://advant.club/auth/login/");
            request.Method = "POST";
            request.ContentType = ContentType;
            request.UserAgent = UserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.Referer = "https://advant.club/auth/login/";
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Origin", "https://advant.club");
            request.ContentLength = content.Length;
            request.AllowAutoRedirect = false;
            request.Headers.Add("Cookie", cookie);
            request.UseDefaultCredentials = false;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //  request.Proxy = new WebProxy(proxy);

            try
            {
                var requestStream = await request.GetRequestStreamAsync();

                byte[] byteArray = Encoding.UTF8.GetBytes(content);
                requestStream.Write(byteArray, 0, content.Length);
                requestStream.Close();

                var response = await request.GetResponseAsync();
            }
            catch (WebException e)
            {
                if (e.Message.Contains("302"))
                {
                    var response = e.Response;
                    cookiesPage = e.Response.Headers["Set-cookie"];
                }
                else
                {
                    Log(e.ToString());
                    return null;
                }
            }

            var rez = regSessId.Match(cookiesPage).Value;

            return rez;
        }

        public async Task<HtmlDocument> GetListCity(string cookie, string proxy)
        {
            HtmlDocument html = new HtmlDocument();
            var time = GetTimeRequest();

            var request = (HttpWebRequest)WebRequest.Create($"https://advant.club/search/departure-city/show/?_={time}");

            request.Method = "GET";
            request.ContentType = ContentType;
            request.UserAgent = UserAgent;
            request.Accept = "*/*";
            request.Referer = "https://advant.club/search";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();

                var stream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(stream, Encoding.UTF8);
                var kodPage = await responseReader.ReadToEndAsync();

                html.LoadHtml(kodPage);

                return html;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        public async Task SetCity(string url, string cookie, string proxy)
        {
            var time = GetTimeRequest();

            var request = (HttpWebRequest)WebRequest.Create($"https://advant.club{url}?_={time}");

            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "*/*";
            request.Referer = "https://advant.club/search";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        public async Task<HtmlDocument> GetStartPage(string flag, string cookie, string proxy)
        {
            HtmlDocument html = new HtmlDocument();

            var request = (HttpWebRequest)WebRequest.Create($"https://advant.club/{flag}/search/");

            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();

                var stream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(stream, Encoding.UTF8);
                var kodPage = await responseReader.ReadToEndAsync();

                html.LoadHtml(kodPage);

                return html;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        public async Task<string> SendFilter(string filter, string cookie, string proxy)
        {
            var request = (HttpWebRequest)WebRequest.Create(filter);

            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Referer = "https://advant.club/search/";
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();
                string newUrl = response.ResponseUri.AbsoluteUri;

                return newUrl;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        public async Task<JToken> LoadHotels(string url, string cookie, string proxy)
        {
            var time = GetTimeRequest();
            var request = (HttpWebRequest)WebRequest.Create($"{url}load/state/?tours=0&hotels=0&_={time}");

            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "application/json, text/javascript, */*; q=0.01";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Referer = url;
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();

                var stream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(stream, Encoding.UTF8);
                var kodPage = await responseReader.ReadToEndAsync();

                var json = JObject.Parse(kodPage);

                return json;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        public async Task<HtmlDocument> GetHotels(string url, string cookie, string proxy)
        {
            var time = GetTimeRequest();
            var request = (HttpWebRequest)WebRequest.Create($"{url}results/?_={time}");

            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "*/*";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Referer = url;
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();

                var stream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(stream, Encoding.UTF8);
                var kodPage = await responseReader.ReadToEndAsync();

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(kodPage);

                return html;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        public async Task<HtmlDocument> GetTours(string url, string cookie, string proxy)
        {
            var time = GetTimeRequest();
            var request = (HttpWebRequest)WebRequest.Create($"{url}?_={time}");

            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "*/*";
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();

                var stream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(stream, Encoding.UTF8);
                var kodPage = await responseReader.ReadToEndAsync();

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(kodPage);

                return html;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        public async Task<HtmlDocument> GetPageTour(string url, string cookie, string proxy)
        {
            var request = (HttpWebRequest)WebRequest.Create($"https://advant.club{url}");

            request.Method = "GET";
            request.UserAgent = UserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Host", "advant.club");
            request.KeepAlive = true;
            request.Headers.Add("Accept-Encoding", AcceptEncoding);
            request.Headers.Add("Accept-Language", AcceptLanguage);
            request.Headers.Add("Cookie", cookie);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Proxy = new WebProxy(proxy);

            try
            {
                var response = await request.GetResponseAsync();

                var stream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(stream, Encoding.UTF8);
                var kodPage = await responseReader.ReadToEndAsync();

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(kodPage);

                return html;
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }


        private long GetTimeRequest()
        {
            DateTime foo = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)foo).ToUnixTimeMilliseconds();

            return unixTime;
        }
        #region Proxy

        public async Task<bool> TestProxy(string ip, int port)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://advant.club/");
            request.Proxy = new WebProxy(ip, port);

            try
            {
                var response = await request.GetResponseAsync();

                var stream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(stream, Encoding.UTF8);
                var kodPage = await responseReader.ReadToEndAsync();

                if (kodPage.Contains("Advant — be successful"))
                    return true;
            }
            catch { }

            return false;
        }

        public async Task<string> DownloadListProxy(string url)
        {
            WebClient client = new WebClient();
            string kodPage = await client.DownloadStringTaskAsync(url);

            return kodPage;
        }

        #endregion
    }
}
