using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Common.Utils;

namespace FindWinner
{
    public class FindWinner
    {
        private readonly ILogger _logger;

        public FindWinner(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FindWinner>();
        }

        [Function("FindWinner")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var stopwatch = Stopwatch.StartNew();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var jsonObject = JObject.Parse(requestBody);
            var rowsArray = jsonObject["data"] as JArray;
            List<string> rows = rowsArray?.ToObject<List<string>>() ?? new List<string>();

            try
            {
                var resultEntries = ConvertResultsAsync(_logger, rows);

                var cleanResultEntries = resultEntries != null && resultEntries.Count > 0
                    ? RemoveDiscrepancies(_logger, resultEntries)
                    : null;

                var finalResults = cleanResultEntries != null && cleanResultEntries.Count > 0
                    ? CalculateFinalResult(_logger, cleanResultEntries)
                    : null;

                if (finalResults != null)
                {
                    var winners = FindWinners(_logger, finalResults);

                    if(winners != null && winners.Count > 0)
                    {
                        string jsonResponse = JsonConvert.SerializeObject(winners);
                        response.WriteString(jsonResponse);
                    }
                    else
                    {
                        string jsonResponse = JsonConvert.SerializeObject("There are no qualified winners.");
                        response.WriteString(jsonResponse);
                    }
                }
                else
                {
                    _logger.LogInformation("There are no valid entries.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during data processing.");
            }

            stopwatch.Stop();
            _logger.LogInformation("Execution Time: {stopwatch.ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            return response;
        }
    }
}
