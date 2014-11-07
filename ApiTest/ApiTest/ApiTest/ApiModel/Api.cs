using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebApiTest.ApiModel
{

    [DataContract]
    public class Api
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Method { get; set; }

        [DataMember]
        public string Url { get; set; }

        //[DataMember]
        //public List<string> UrlParameters { get; set; }

        [DataMember]
        public string DataParameters { get; set; }
    }

}
