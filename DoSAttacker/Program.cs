using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DoSAttacker {
	internal class Program {
		private static void Main(string[] args) {
			const string baseAddress = "http://localhost:55555/";
			var max = args.Count() > 1 ? Convert.ToInt32(args[1]) : 20;
			SendRequests(baseAddress, max);

			Console.ReadKey();
		}

		private static void SendRequests(string baseAddress, int max) {
			foreach (var sleep in new[] { 1, 250, 500, 1000 }) {
				var tasks = new List<Task>();
				var responses = new ConcurrentBag<WebResponse>();
				var requestUrl = baseAddress + "home/index?sleep=" + sleep;
				Console.WriteLine("Requesting '{0}' {1} times...", requestUrl, max);
				var stopwatch = Stopwatch.StartNew();
				for (var i = 0; i < max; i++) {
					tasks.Add(
						Task.Factory.StartNew(() => responses.Add(
							WebRequest.CreateDefault(
								new Uri(requestUrl))
								.GetResponse())));
					Console.Write(".");
				}
				Task.WaitAll(tasks.ToArray());
				stopwatch.Stop();

				Console.WriteLine();
				Console.WriteLine("Requested '{0}' {1} times in {2}.", requestUrl, max, stopwatch.Elapsed.Duration());
				Console.WriteLine("First response:");
				var responseList = responses.ToList();
				WriteDebug(responseList[0]);
				var nr = max/2 - 1;
				Console.WriteLine("Response {0}:", nr);
				WriteDebug(responseList[nr]);
				nr = max - 1;
				Console.WriteLine("Response {0}:", nr);
				WriteDebug(responseList[nr]);
			}
		}

		private static void WriteDebug(WebResponse webResponse) {
			foreach (var key in webResponse.Headers.AllKeys) {
				Console.WriteLine("\t{0}:\t {1}", key, webResponse.Headers[key]);
			}
			using (var stream = webResponse.GetResponseStream())
				try {
					using (var reader = new StreamReader(stream)) {
						Console.WriteLine("\t" + reader.ReadToEnd().Replace(Environment.NewLine, "\t" + Environment.NewLine));
					}
				}
				catch (ArgumentException e) {
					Console.WriteLine("\tException: {0}\r\n{1}", e.Message,
					                  e.StackTrace.Replace(Environment.NewLine, "\t" + Environment.NewLine));
				}
		}
	}
}