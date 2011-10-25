using System;
using System.Collections.Concurrent;
using System.Web;
using System.Web.SessionState;

namespace DoSAttack.HttpModules {
	public class DosPreventionModule : IHttpModule {
		private HttpSessionState _session;
		private string _ipAddress;
		private string _sessionId;
		private string _cacheKey;
		private DateTime _reqTime;

		public void Init(HttpApplication context) {
			context.BeginRequest += OnApplicationBeginRequest;
			context.EndRequest += OnApplicationEndRequest;
			context.PostAcquireRequestState += context_PostAcquireRequestState;
		}

		private void context_PostAcquireRequestState(object sender, EventArgs e) {
			var application = (HttpApplication) sender;
			_session = application.Session;
			_sessionId = _session.SessionID;

			if (RequestCountExceedsMax(application))
				CancelRequest(application);

			_session["init"] = "init";
		}

		private void CancelRequest(HttpApplication application) {
			application.Response.Write("Request count exceeded!");
			application.Response.End();
		}

		private bool RequestCountExceedsMax(HttpApplication application) {
			_cacheKey = string.Format("{0}-{1}", GetIpAddress(application.Request), _sessionId);
			var dict = application.Context.Cache[_cacheKey] as ConcurrentDictionary<DateTime, int>;
			if (dict == null) {
				dict = new ConcurrentDictionary<DateTime, int>();
				application.Context.Cache[_cacheKey] = dict;
			}
			dict.AddOrUpdate(
				new DateTime(_reqTime.Year, _reqTime.Month, _reqTime.Day, _reqTime.Hour, _reqTime.Minute, _reqTime.Second),
				d => 1, (date, value) => value + 1);

			return AverageOverTimeExceedsMax(dict, TimeSpan.FromMinutes(1));
		}

		private bool AverageOverTimeExceedsMax(ConcurrentDictionary<DateTime, int> requestCounts, TimeSpan timeSpan) {
			var max = 15;
			var now = DateTime.Now;
			var sum = 0;
			foreach (var dateTime in requestCounts.Keys) {
				if (now - timeSpan <= dateTime) {
					sum += requestCounts[dateTime];
					if (sum > max)
						return true;
				}
				else {
					int tmp;
					requestCounts.TryRemove(dateTime, out tmp);
				}
			}
			return false;
		}

		private void OnApplicationBeginRequest(object sender, EventArgs e) {
			var application = (HttpApplication) sender;
			_ipAddress = GetIpAddress(application.Request);
			application.Application["Module run"] = "true";
			_reqTime = DateTime.Now;
		}

		private void OnApplicationEndRequest(object sender, EventArgs e) {
			var application = (HttpApplication) sender;
			//_session = application.Session;
			//_ipAddress = GetIpAddress(application.Request);
			application.Response.AddHeader("Session", _session != null ? _sessionId : "null");
			application.Response.AddHeader("IpAddress", _ipAddress);
			var dict = application.Context.Cache[_cacheKey] as ConcurrentDictionary<DateTime, int>;
			foreach (var dateTime in dict.Keys)
				application.Response.Write(string.Format("<p>{0} -> {1} requests</p>", dateTime.ToString("HH:mm:ss fffffff"),
				                                         dict[dateTime]));
		}

		public void Dispose() {
		}

		public static string GetIpAddress(HttpRequest request) {
			var ip = request.UserHostAddress;

			if (!string.IsNullOrEmpty(ip))
				return ip;

			// See: http://forums.asp.net/p/1053767/1496008.aspx

			ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			// If there is no proxy, get the standard remote address

			if (string.IsNullOrEmpty(ip) || (ip.ToLowerInvariant() == "unknown"))
				ip = request.ServerVariables["REMOTE_ADDR"];

			return ip ?? "unknown";
		}
	}
}