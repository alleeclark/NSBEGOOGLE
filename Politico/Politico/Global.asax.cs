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
        const string TWITTERAPPACCESSTOKEN = "<Enter Twitter App Access Token>";
        const string TWITTERAPPACCESSTOKENSECRET = "<Enter Twitter Access Token Secret>";
        const string TWITTERAPPAPIKEY = "<Enter Twitter App API Key>";
        const string TWITTERAPPAPISECRET = "<Enter Twitter App API Secret>";
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
