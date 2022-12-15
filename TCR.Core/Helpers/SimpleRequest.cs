using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TCRCore.Helpers
{
    public static class SimpleRequest
    {
        //public static HttpWebRequest CreateRequest(string uri)
        //=> CreateRequest(new Uri(uri));

        /// <summary>
        /// Generates a legacy POST request of type "application/json". Sends the JSON to the requested Uri,
        /// and returns the response.
        /// </summary>
        /// <param name="uri">Uri endpoint to send JSON to.</param>
        /// <param name="headers">Headers to be included when sending the JSON.</param>
        /// <param name="json">JSON to be sent.</param>
        /// <returns>String response from server.</returns>
        public static async Task<string> SendJsonDataAsync(string uri, WebHeaderCollection headers, string json)
            => await SendJsonDataAsync(new Uri(uri), headers, json);

        /// <summary>
        /// Generates a legacy POST request of type "application/json". Sends the JSON to the requested Uri,
        /// and returns the response.
        /// </summary>
        /// <param name="uri">Uri endpoint to send JSON to.</param>
        /// <param name="headers">Headers to be included when sending the JSON.</param>
        /// <param name="json">JSON to be sent.</param>
        /// <returns>String response from server.</returns>
        public static async Task<string> SendJsonDataAsync(Uri uri, WebHeaderCollection headers, string json)
        {
            var methodType = "POST";
            var contentType = "application/json";
            var content = await new StringContent(json, Encoding.UTF8, contentType).ReadAsByteArrayAsync();

			try
			{
                return await SendRequestAsync(uri, headers, methodType, contentType, content);
            }
			catch
			{
                PrettyPrint.Log("Discord", "Fatal error attempting to send data", ConsoleColor.Red);
                return "";
			}
        }

        /// <summary>
        /// Generates a legacy request. Sends the content to the requested Uri,
        /// and returns the response.
        /// </summary>
        /// <param name="uri">Uri endpoint to send content to.</param>
        /// <param name="headers">Headers to be included when sending the content.</param>
        /// <param name="methodType">Method type of the request. Examples: POST, GET, PUT, etc.</param>
        /// <param name="contentType">The content's type.</param>
        /// <param name="content">Byte array content to be sent</param>
        /// <exception cref="WebException"></exception>
        /// <returns>String response from server.</returns>
        public static async Task<string> SendRequestAsync(Uri uri, WebHeaderCollection headers, string methodType, string contentType, byte[] content)
        {
            HttpWebRequest webRequest = HttpWebRequest.CreateHttp(uri);
            webRequest.Method = methodType;
            webRequest.ContentType = contentType;
            webRequest.ContentLength = content.Length;

            // HttpWebRequest.CreateHttp adds it's own keys. Instead of overwriting them, we'll add onto them.
            foreach (var header in headers.AllKeys)
            {
                webRequest.Headers.Add($"{header}: {headers[header]}");
                // Console.WriteLine($"{header}: {headers[header]}");
            }

			try
            {
                var reqStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false);

                if(reqStream != null)
				{

                    reqStream.Write(content, 0, content.Length);
                    var res = await webRequest.GetResponseAsync().ConfigureAwait(false);
                    using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (WebException e)
			{
                using (StreamReader sr = new StreamReader(e.Response.GetResponseStream()))
                {
                    Console.WriteLine("Error sending request: " + sr.ReadToEnd());
                }
			}
            catch (Exception e)
			{
                Console.WriteLine("Fatal error sending request: " + e.Message);
            }
            return null;
        }
    }
}
