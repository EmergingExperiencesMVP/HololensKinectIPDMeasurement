using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HololensIPDMeasurementTool
{
    public class DevPortalHelper
    {
        private DevPortalVM _settings;
        public DevPortalHelper(DevPortalVM settings)
        {
            _settings = settings;

            ServicePointManager
            .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }
        /// <summary>
        /// Configures and returns an HTTP client that doesn't care about certificate failures, plus sets up authorization
        /// </summary>
        private HttpClient CreateIgnorantHttpClient()
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Base64Token);
            return client;
        }

        public Uri ipdUri {
            get
            {
                var baseUriString = "https://{0}/api/holographic/os/settings/ipd";
                if (_settings.UseUSB)
                {
                    return new Uri(string.Format(baseUriString, "127.0.0.1"));
                }else
                {
                    return new Uri(string.Format(baseUriString, _settings.IpAddress));
                }
            }
        }

        private string Base64Token
        {
            get
            {
                var byteArray = Encoding.ASCII.GetBytes($"{_settings.UserName}:{_settings.Password}");
                var base64Token = Convert.ToBase64String(byteArray);
                return base64Token;
            }
        }


        //parses a string like: {"ipd" : 53000}
        private double GetIPDFromResult(string result)
        {
            var value = result.Split(':')[1].Trim(' ', '}');
            return Double.Parse(value);
        }



        public void SetIPD(double ipd)
        {
            var client = CreateIgnorantHttpClient();
            var uriBuilder = new UriBuilder(ipdUri);
            uriBuilder.Query = "ipd=" + ipd * 1000;
            var response = client.PostAsync(uriBuilder.Uri, null);
            var me = response.Result.StatusCode;
            _settings.IPD = ipd;
        }

        public async Task GetIPDFromDevice()
        {
            double ipd;
            try
            {
                var client = CreateIgnorantHttpClient();
                var response = await client.GetStringAsync(ipdUri);
                var res = GetIPDFromResult(response);
                ipd = res / 1000;
                Debug.WriteLine("IPD: " + ipd);
            }
            catch (Exception)
            {
                return;
            }
            _settings.IPD = ipd;
        }

    }
}
