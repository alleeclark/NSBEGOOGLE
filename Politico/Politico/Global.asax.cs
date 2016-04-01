using AngularJSWebApiEmpty.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using Tweetinvi;

namespace Politico
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        const string TWITTERAPPACCESSTOKEN = "4247602527-6YiPquogfC1zbNa1tS7nvnALkRzQJ37S8TqlMs8";
        const string TWITTERAPPACCESSTOKENSECRET = " Z3yvPPieLWrEkEYvhy882hCEttXlhfpM7O6sXz6PBlUiu";
        const string TWITTERAPPAPIKEY = " G14oacGN55sEjZQJP0jpsIEsP";
        const string TWITTERAPPAPISECRET = " Htm3LwLCblNPLuQDyUyNhMeMRlyz7ekj8oGRtYEtM3StliGe61";
        protected void Application_Start()
        {
            Auth.SetUserCredentials(TWITTERAPPAPIKEY, TWITTERAPPAPISECRET, TWITTERAPPACCESSTOKEN, TWITTERAPPACCESSTOKENSECRET);

            Stream_FilteredStreamExample();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            

        }
        private static void Stream_FilteredStreamExample()
        {
            for (;;)
            {
                try
                {
                    HBaseWriter hbase = new HBaseWriter();
                    var stream = Stream.CreateFilteredStream();
                    //stream.AddLocation(new Coordinates(-180, -90), new Coordinates(180, 90)); //Geo .GenerateLocation(-180, -90, 180, 90));

                    var tweetCount = 0;
                    var timer = Stopwatch.StartNew();

                    stream.MatchingTweetReceived += (sender, args) =>
                    {
                        tweetCount++;
                        var tweet = args.Tweet;

                        // Write Tweets to HBase
                        hbase.WriteTweet(tweet);

                        if (timer.ElapsedMilliseconds > 1000)
                        {
                            if (tweet.Hashtags != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("\n{0}: {1} {2}", tweet.Id, tweet.Language.ToString(), tweet.Text);
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine("\tLocation: {0}", tweet.Hashtags);
                            }

                            timer.Restart();
                            Console.WriteLine("\tTweets/sec: {0}", tweetCount);
                            tweetCount = 0;
                        }
                    };

                    stream.StartStreamMatchingAllConditions();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0}", ex.Message);
                }
            }
        }

    }
}
