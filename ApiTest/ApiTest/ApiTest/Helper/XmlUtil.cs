using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WebApiTest.Helper
{
    public static class XmlUtil
    {
        public static object Deserialize(Type type, string xmlpath)
        {
            try
            {
                if (!File.Exists(xmlpath))
                    return null;
                using (StreamReader reader = new StreamReader(xmlpath))
                {
                    XmlSerializer xs = new XmlSerializer(type);
                    object obj = xs.Deserialize(reader);
                    reader.Close();
                    return obj;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
