using System.Threading;
using System.Web.Mvc;

namespace DoSAttack.Controllers {
	public class HomeController : Controller {
		//
		// GET: /Home/

		public ActionResult Index(int sleep = 500) {
			Thread.Sleep(sleep);
			return View();
		}
	}
}