using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebScraping.Models;

namespace WebScraping.Services
{
    public class ScrapingService
    {
        private readonly ILogger<ScrapingService> _logger;

        private static IMemoryCache memoryCache;
        private static MemoryCacheEntryOptions entryOptions;

        private readonly IHttpClientFactory _clientFactory;
        private HttpClient client;

        private const string BaseUrl = "https://github.com/";
        private readonly IConfiguration config;
        private readonly IBrowsingContext browsingContext;

        private List<FileRepository> results;

        public ScrapingService(
            ILogger<ScrapingService> logger,
            IMemoryCache memoryCacheParam,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;

            if (memoryCache == null)
            {
                memoryCache = memoryCacheParam;
                this.SetMemoryCacheEntryOptions();
            }

            config = Configuration.Default.WithDefaultLoader();
            browsingContext = BrowsingContext.New(config);

            _clientFactory = clientFactory;

            client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
        }

        private void SetMemoryCacheEntryOptions()
        {
            entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddHours(24),
                Priority = CacheItemPriority.Low
            };
        }

        private void Set<T>(string chave, T valor) => memoryCache.Set(chave, valor, entryOptions);

        private static double GetValue(string value, double sizeInBytes) => value switch
        {
            "kb" => sizeInBytes *= 1000,
            "mb" => sizeInBytes *= 1000000,
            "gb" => sizeInBytes *= 1e+9,
            _ => sizeInBytes,
        };

        private async Task GetBobyRepoToListResultAsync(string endpoint, bool isBlob = false)
        {
            var response = await client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                throw new ArgumentNullException("Não foi possivel encontrar o endpoint especificado");
            }

            var content = await response.Content.ReadAsStringAsync();

            var document = await browsingContext.OpenAsync(req => req.Content(content));

            if (isBlob)
            {
                var lastTd = document.QuerySelectorAll("table.highlight tbody tr").LastOrDefault();

                var header = document.QuerySelector("div.Box-header div.text-mono");

                var existente = results.FirstOrDefault(r => r.UrlBlob == endpoint);
                if (existente != null)
                {
                    var textLower = header.Text().Replace("\n", "").Replace("  ", "").ToLower();
                    var valueSplit = textLower.Substring(textLower.LastIndexOf(")") + 1).Split(" ", StringSplitOptions.RemoveEmptyEntries);

                    var valueSize = valueSplit[0];
                    var valueSizeOption = valueSplit[1];

                    var sizeInBytes = GetValue(valueSizeOption, double.Parse(valueSize));

                    existente.CountLines = lastTd != null ? int.Parse(lastTd.QuerySelector("td").GetAttribute("data-line-number").Trim()) : 0;
                    existente.ValueSize = sizeInBytes;
                    existente.Type = existente.FileName.Contains(".") ? existente.FileName.Substring(existente.FileName.LastIndexOf(".")) : existente.FileName;
                }
            }
            else
            {
                foreach (var selector in document.QuerySelectorAll("[aria-labelledby^='files'] div.Box-row"))
                {
                    var anchor = selector.QuerySelector("div[role='rowheader'] span a");

                    if (anchor != null)
                    {
                        var result = new FileRepository
                        {
                            FileName = anchor.Text(),
                            UrlBlob = anchor.GetAttribute("href")
                        };

                        var blobFile = result.UrlBlob.Contains("/blob/");

                        if (blobFile)
                        {
                            results.Add(result);
                        }

                        await GetBobyRepoToListResultAsync(result.UrlBlob, blobFile);
                    }
                }
            }
        }

        private async Task<IEnumerable<ViewModel>> GetFileResults(string endpoint)
        {
            try
            {
                results = new List<FileRepository>();

                await GetBobyRepoToListResultAsync(endpoint);

                var group = results.GroupBy(g => g.Type);

                var list = new List<ViewModel>();

                foreach (var item in group)
                {
                    list.Add(new ViewModel
                    {
                        Type = item.Key,
                        ValueSize = item.Sum(s => s.ValueSize),
                        CountLines = item.Sum(s => s.CountLines)
                    });
                }

                return list.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"Error get values from github for endpoint: {endpoint}", ex);
                throw;
            }
        }

        public async Task<IEnumerable<ViewModel>> GetResultRepositoryAsync(string endpoint)
        {
            if (memoryCache.TryGetValue(endpoint, out IEnumerable<ViewModel> outValue))
            {
                return outValue;
            }
            else
            {
                var list = await GetFileResults(endpoint);
                Set(endpoint, list);

                return list.ToList();
            }
        }
    }
}
