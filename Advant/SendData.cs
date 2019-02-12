using System;
using System.Collections.Generic;
using System.Text;

namespace Advant
{
    public class SendData
    {
        public AdvantWeb Web { get; set; }
        public string Cookie { get; set; }
        public string Proxy { get; set; }

        public SendData()
        {
        }

        public SendData(AdvantWeb web, string cookie, string proxy)
        {
            Web = web;
            Cookie = cookie;
            Proxy = proxy;
        }
    }
}
