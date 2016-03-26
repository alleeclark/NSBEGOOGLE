using Microsoft.HBase.Client;
using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;

namespace AngularJSWebApiEmpty.Models
{
    class HBaseReader
    {
        HBaseClient client;

        const string CLUSTERNAME = "BlackLives.azurehdinsight.net <https://BlackLives.azurehdinsight.net>";
        const string HADOOPUSERNAME = "admin";
        const string HADOOPPASSWORD = "HU@02668218l";
        const string HBASETABLENAME = "blacktweets";

        public HBaseReader()
        {
            ClusterCredentials credentials = new ClusterCredentials(new Uri(CLUSTERNAME), HADOOPUSERNAME, HADOOPPASSWORD);
            client = new HBaseClient(credentials);
        }

        public async Task<IEnumerable<Tweet>> QueryTweetsByKeywordAsync(string keyword)
        {
            List<Tweet> list = new List<Tweet>();

            string timeIndex = (ulong.MaxValue - (ulong)DateTime.UtcNow.Subtract(new TimeSpan(1800, 0, 0)).ToBinary()).ToString().PadLeft(20);
            string startRow = keyword + "_" + timeIndex;
            string endRow = keyword + "|";
            Scanner scanSettings = new Scanner
            {
                batch = 1000000,
                startRow = Encoding.UTF8.GetBytes(startRow),
                endRow = Encoding.UTF8.GetBytes(endRow)
            };
            ScannerInformation scannerInfo = await client.CreateScannerAsync(HBASETABLENAME, scanSettings);
            CellSet next;

            while ((next = await client.ScannerGetNextAsync(scannerInfo)) != null)
            {
                foreach (CellSet.Row row in next.rows)
                {

                    var blackliveshashtag = row.values.Find(c => Encoding.UTF8.GetString(c.column) == "d:tags");

                    if (blackliveshashtag != null)
                    {
                        string[] hastags = Encoding.UTF8.GetString(blackliveshashtag.data).Split(',');
                        var sentimentField = row.values.Find(c => Encoding.UTF8.GetString(c.column) == "d.sentiment");
                        Int32 sentiment = 0;
                        if (sentimentField != null)
                        {
                            sentiment = Convert.ToInt32(Encoding.UTF8.GetString(sentimentField.data));
                        }

                        list.Add(new Tweet
                        {
                            //Longtitude = Convert.ToDouble(long[0]),
                            Hashtags = Convert.ToString(hastags[0]),
                            Sentiment = sentiment
                        });
                    }
                    if (blackliveshashtag != null)
                    {
                        string[] hashtags = Encoding.UTF8.GetString(blackliveshashtag.data).Split(',');
                    }

                }
            }
            return list;
        }
    }
    public class Tweet
    {
        public string IdStr { get; set; }
        public string Text { get; set; }
        public string Hashtags { get; set; }
        public int Sentiment { get; set;}
    }
}
