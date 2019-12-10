﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CRM_MVC.Common
{
    public abstract class HttpClientService<T> : IDisposable where T : class
    {
        private HttpClient _httpClient;
        private readonly UrlService _urlService;
        private readonly ILogger<HttpClientService<T>> _logger;

        /// <summary>
        /// urlService、logger使用依赖注入
        /// </summary>
        /// <param name="urlService"></param>
        /// <param name="logger"></param>
        protected HttpClientService(UrlService urlService, ILogger<HttpClientService<T>> logger)
        {
            _urlService = urlService ??
                throw new ArgumentNullException(nameof(urlService));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            // 创建HttpClient
            _httpClient = new HttpClient();
            // 使用从UrlService检索的基址
            _httpClient.BaseAddress = new Uri(_urlService.BaseAddress);
        }

        private async Task<string> GetInternalAsync(string requestUri)
        {
            if (requestUri == null)
                throw new ArgumentNullException(nameof(requestUri));
            if (_objectDisposed)
                throw new ObjectDisposedException(nameof(_httpClient));

            HttpResponseMessage resp = await _httpClient.GetAsync(requestUri);
            Console.WriteLine($"GET状态{resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 返回单个泛型对象
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public async virtual Task<T> GetAsync(string requestUri)
        {
            if (requestUri == null)
                throw new ArgumentNullException(nameof(requestUri));

            string json = await GetInternalAsync(requestUri);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// 返回多个泛型对象。
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public async virtual Task<IEnumerable<T>> GetAllAsync(string requestUri)
        {
            if (requestUri == null)
                throw new ArgumentNullException(nameof(requestUri));

            string json = await GetInternalAsync(requestUri);
            return JsonConvert.DeserializeObject<IEnumerable<T>>(json);
        }

        /// <summary>
        /// 返回全部对象(Xml格式)
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public async Task<XElement> GetAllXmlAsync(string requestUri)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_urlService.BaseAddress);
                client.DefaultRequestHeaders.Accept.Add(
                  new MediaTypeWithQualityHeaderValue("application/xml"));
                HttpResponseMessage resp = await client.GetAsync(requestUri);
                Console.WriteLine($"GET状态{resp.StatusCode}");
                resp.EnsureSuccessStatusCode();
                string xml = await resp.Content.ReadAsStringAsync();
                XElement chapters = XElement.Parse(xml);
                return chapters;
            }
        }

        /// <summary>
        /// Post 返回
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<T> PostAsync(string uri, T item)
        {
            if (_objectDisposed) throw new ObjectDisposedException(nameof(_httpClient));

            string json = JsonConvert.SerializeObject(item);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage resp = await _httpClient.PostAsync(uri, content);
            Console.WriteLine($"POST状态 {resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
            Console.WriteLine($"添加资源在{resp.Headers.Location}");
            json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task PutAsync(string uri, T item)
        {
            if (_objectDisposed) throw new ObjectDisposedException(nameof(_httpClient));

            string json = JsonConvert.SerializeObject(item);
            HttpContent content = new StringContent(json, Encoding.UTF8,
              "application/json");
            HttpResponseMessage resp = await _httpClient.PutAsync(uri, content);
            Console.WriteLine($"PUT 状态{resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string uri)
        {
            if (_objectDisposed) throw new ObjectDisposedException(nameof(_httpClient));

            HttpResponseMessage resp = await _httpClient.DeleteAsync(uri);
            Console.WriteLine($"DELETE 状态{resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
        }

        #region IDisposable Support
        private bool _objectDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_objectDisposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _objectDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
