using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PassthroughWebAPI.Models
{
    public class PassthroughResponse
    {
        public PassthroughResponse()
        { }

        public PassthroughResponse(string refID)
        {
            REF = refID;
        }

        public string REF { get; set; }
    }
}