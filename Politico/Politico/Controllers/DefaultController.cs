using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tweetinvi;

namespace AngularJSWebApiEmpty.Controllers
{
    public class DefaultController : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
            //Auth.SetUserCredentials(Twi)
            return View();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            //const string TWITTERAPPACCESSTOKEN = "";
            //const string TWITTERAPPACCESSTOKENSECRET = "";
            //const string TWITTERAPPAPIKEY = "";
            //const string TWITTERAPPAPISECRETY = "";
            //UseTwitterAuthentication(new twitterAuthenticationOptions()
            //var user = User.GetUserFromScreenName("<username>");
        }
    }
}