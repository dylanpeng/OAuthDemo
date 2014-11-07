using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OAuthTest.Common
{
    public static class XmlFileHelper
    {
        public static void SaveXml(string filePath, object obj, System.Type type)
        {
            using (var writer = new System.IO.StreamWriter(filePath))
            {
                var xs = new System.Xml.Serialization.XmlSerializer(type);
                xs.Serialize(writer, obj);
                writer.Close();
            }
        }

        public static object LoadXml(string filePath, System.Type type)
        {
            if (!System.IO.File.Exists(filePath))
                return null;
            using (var reader = new System.IO.StreamReader(filePath))
            {
                var xs = new System.Xml.Serialization.XmlSerializer(type);
                object obj = xs.Deserialize(reader);
                reader.Close();
                return obj;
            }
        }

        public static void SaveXml<T>(string filePath, T obj)
        {
            using (var writer = new System.IO.StreamWriter(filePath))
            {
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                xs.Serialize(writer, obj);
                writer.Close();
            }
        }

        public static T LoadXml<T>(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return default(T);
            using (var reader = new System.IO.StreamReader(filePath))
            {
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                var obj = xs.Deserialize(reader);
                reader.Close();
                return (T)obj;
            }
        }
    }
}