using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PassthroughWebAPI.Models
{
    public class PassthroughRequest
    {
        public PassthroughRequest()
        { }

        public string subject { get; set; }

        public string SecretGUIDPhrase { get; set; }
        public RequestPayload payload { get; set; }
    }

    public class RequestPayload
    {
        public RequestPayload()
        { }

        public string sessEmail { get; set; }
        public string cFname { get; set; }
        public string cLname { get; set; }
        public string cEmail { get; set; }

        public string cStatus { get; set; }
        //public string cUname { get; set; }
        public string cTou { get; set; }
        public string cSLAName { get; set; }
        public long amID { get; set; }
        public int deptID { get; set; }


    }
}