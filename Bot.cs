using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Blitz
{
    [ApiController]
    [Route("[controller]")]
    public class MyBot : ControllerBase
    {
        [HttpPost("/microchallenge")]
        public ActionResult Solve([FromBody] Problem problem)
        {
            var result = new int[problem.items.Length];
            for (var i = 0; i < problem.items.Length; i++)
            {
                var source = problem.items[i][0];
                var destination = problem.items[i][1];

                if (source < destination)
                {
                    for (int j = source; j < destination; ++j)
                        result[i] += problem.track[j];
                }
                else if (source > destination)
                {
                    for (int j = source - 1; j >= destination; --j)
                        result[i] += problem.track[j];
                }
            }

            return Ok(result);
        }
    }

    public class Problem
    {
        public int[][] items { get; set; }
        public int[] track { get; set; }

        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}