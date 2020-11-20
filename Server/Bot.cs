using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Blitz.Server
{
    [ApiController]
    [Route("[controller]")]
    public class MyBot : ControllerBase
    {
        [HttpPost("/microchallenge")]
        public async Task Solve()
        {
            HttpContext.Response.StatusCode = 200;
            HttpContext.Response.Headers.Add("Content-Type", "application/json");
            await HttpContext.Response.StartAsync();

            await using var writer = new Utf8JsonWriter(HttpContext.Response.Body);
                
            using var document = await JsonDocument.ParseAsync(HttpContext.Request.Body);

            var trackElement = document.RootElement.GetProperty("track");
            var track = new int[trackElement.GetArrayLength()];
            {
                int i = 0;
                foreach (var elem in trackElement.EnumerateArray())
                    track[i++] = elem.GetInt32();
            }

            var problemElement = document.RootElement.GetProperty("items");

            var problems = new (int, int)[problemElement.GetArrayLength()];
            {
                int i = 0;
                foreach (var problem in problemElement.EnumerateArray())
                {
                    int source;
                    int destination;
                    using (var e = problem.EnumerateArray())
                    {
                        e.MoveNext();
                        source = e.Current.GetInt32();
                        e.MoveNext();
                        destination = e.Current.GetInt32();
                    }

                    problems[i++] = (source, destination);
                }
            }

            var results = new int[problems.Length];
            Parallel.For(0, results.Length, index =>
            {
                var (source, destination) = problems[index];
                int total = 0;
                if (source < destination)
                {
                    for (var j = source; j < destination; ++j)
                        total += track[j];
                }
                else if (source > destination)
                {
                    for (var j = source - 1; j >= destination; --j)
                        total += track[j];
                }
                results[index] = total;
            });

            writer.WriteStartArray();
            foreach (var result in results) 
                writer.WriteNumberValue(result);
            writer.WriteEndArray();
        }
    }
}