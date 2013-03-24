using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace RoutingSample.Tests
{
	/// <summary>
	/// A message handler for simulating web requests
	/// </summary>
	public class TestMessageHandler : HttpMessageHandler
	{
		//Holds predefined test responses in TestResponses.resw
		private static ResourceLoader s_TestResponses;
		public static TestMessageHandler FromResourceResponse(string key)
		{
			if(s_TestResponses == null)
				s_TestResponses = new Windows.ApplicationModel.Resources.ResourceLoader("TestResponses");
			var testResponse = s_TestResponses.GetString(key);
			return new TestMessageHandler(testResponse);
		}

		private HttpResponseMessage m_response;
		private Exception m_exception;

		/// <summary>
		/// Always returns the provided response when using the message handler as StringContent.
		/// </summary>
		/// <param name="response"></param>
		public TestMessageHandler(string response)
		{
			m_response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new System.Net.Http.StringContent(response)
			};
		}

		/// <summary>
		/// Always returns the provided response when using the message handler.
		/// </summary>
		/// <param name="response"></param>
		public TestMessageHandler(HttpContent response)
		{
			m_response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = response
			};
		}

		/// <summary>
		/// Always returns the provided response when using the message handler.
		/// </summary>
		/// <param name="response"></param>
		public TestMessageHandler(HttpResponseMessage response)
		{
			m_response = response;
		}

		/// <summary>
		/// Throws the provided exception when using this message handler
		/// </summary>
		/// <param name="ex"></param>
		public TestMessageHandler(Exception ex)
		{
			m_exception = ex;
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			await Task.Delay(1).ConfigureAwait(continueOnCapturedContext: false); //Simulate the request time slightly
			if (m_exception != null)
				throw m_exception;
			else
				return m_response;
		}
	}
}
