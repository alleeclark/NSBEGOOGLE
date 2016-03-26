using AngularJSWebApiEmpty.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace AngularJSWebApiEmpty.Controllers
{
    public class TweetingController : TweetingController
    {
        HBaseReader hbase = new HBaseReader();
        public async Task<IEnumerable<Tweet>> GetTweetsByQuery(string query)
        {
            return await hbase.QueryTweetsByKeywordAsync(query);
        }
    }
}
