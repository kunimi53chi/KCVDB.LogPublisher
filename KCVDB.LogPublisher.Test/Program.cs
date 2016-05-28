using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using KCVDB.LocalAnalyze;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KCVDB.LogPublisher.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            new LogDirectory(@"D:\BlobDataDownloader\Publish\2016-05-03").Subscribe(logFile =>
            {
                Console.WriteLine(logFile.SessionId);
                logFile.Subscribe(line =>
                {
                    var row = new KCVDBRow(line);
                    var query = HttpUtility.ParseQueryString(row.RequestValue);
                    dynamic svdata = JsonConvert.DeserializeObject(row.ResponseValue.Replace("svdata=", ""));
                    //Read(null, svdata);
                });
            });
        }

        static void Read(dynamic key, JObject value)
        {
            foreach (var pair in value)
            {
                Read(pair.Key, (dynamic)pair.Value);
            }
        }

        static void Read(dynamic key, JArray value)
        {
            for (var i = 0; i < value.Count; ++i)
            {
                Read(i, (dynamic)value[i]);
            }
        }

        static void Read(dynamic key, JValue value)
        {
            Console.WriteLine($"{key.GetType()}, {key}, {value.GetType()}, {value}");
        }
}
