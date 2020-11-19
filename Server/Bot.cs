using System.Collections.Generic;
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

            await using var writer = new Utf8JsonWriter(HttpContext.Response.Body);
            await HttpContext.Response.StartAsync();

            writer.WriteStartArray();
                
            using var document = await JsonDocument.ParseAsync(HttpContext.Request.Body);

            var trackElement = document.RootElement.GetProperty("track");
            var track = new List<int>(trackElement.GetArrayLength());
            foreach (var elem in trackElement.EnumerateArray())
                track.Add(elem.GetInt32());

            var problemElement = document.RootElement.GetProperty("items");
                
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

                writer.WriteNumberValue(total);
            }

            writer.WriteEndArray();
        }
    }
}