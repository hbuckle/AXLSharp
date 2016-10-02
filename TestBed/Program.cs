using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBed
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new AXLSharp.AXLPortClient();
            service.ClientCredentials.UserName.UserName = "administrator";
            service.ClientCredentials.UserName.Password = "ciscopsdt";

            var getlinereq = new AXLSharp.GetLineReq();
            getlinereq.Items = new object[1];
            getlinereq.Items[0] = "1006";
            getlinereq.ItemsElementName = new AXLSharp.ItemsChoiceType57[1];
            getlinereq.ItemsElementName[0] = AXLSharp.ItemsChoiceType57.pattern;
            var result = service.getLine(getlinereq);
        }
    }
}