using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blitz.TestClient
{
    public static class Program
    {
        public class Problem
        {
            public int[][] items { get; set; }
            public int[] track { get; set; }
        }

        public static async Task Main(string[] args)
        {
            Problem[] testCases;
            await using (var stream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.Read)) 
                testCases = await JsonSerializer.DeserializeAsync<Problem[]>(stream);

            using var client = new HttpClient { BaseAddress = new Uri("http://localhost:27178") };

            var scores = new List<double>();

            int i = 0;
            foreach (var testCase in testCases)
            {
                var body = JsonSerializer.Serialize(testCase);

                var sw = Stopwatch.StartNew();
                using var response = await client.PostAsync("microchallenge", new StringContent(body));
                var time = sw.Elapsed;

                var value = 0.0;
                if (response.IsSuccessStatusCode)
                {
                    value = Math.Max(0.0, 3.0 - time.TotalSeconds);
                }

                scores.Add(value);
                Console.WriteLine("Test {0}: {1}", ++i, value);
            }

            var total = scores.Sum();
            var avg = scores.Average();

            Console.WriteLine("Total score: {0}", total);
            Console.WriteLine("Average score: {0}", avg);
            Console.ReadKey(true);
        }
    }
}
