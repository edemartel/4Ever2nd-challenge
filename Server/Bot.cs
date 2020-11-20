using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Blitz.Server
{
    [ApiController]
    [Route("[controller]")]
    public class MyBot : ControllerBase
    {
        private enum State
        {
            Initial,
            Items,
            Source,
            Destination,
            Track
        }

        private class Context : IDisposable
        {
            public State State = State.Initial;
            public readonly int[] Track;
            public int CurrentTrackIndex;
            public readonly (int, int)[] Problems;
            public int CurrentProblemIndex;
            public int CurrentSource;
            public State NextState = State.Initial;

            public Context()
            {
                Track = ArrayPool<int>.Shared.Rent(10000);
                Problems = ArrayPool<(int, int)>.Shared.Rent(100000);
            }

            public void Dispose()
            {
                ArrayPool<(int, int)>.Shared.Return(Problems);
                ArrayPool<int>.Shared.Return(Track);
            }
        }

        private static SequencePosition ReadNext(Context context, in ReadResult readResult, ref JsonReaderState state)
        {
            var json = new Utf8JsonReader(readResult.Buffer, readResult.IsCompleted, state);

            Span<char> propertyBuffer = stackalloc char[8];

            while (json.Read())
            {
                switch (json.TokenType)
                {
                    case JsonTokenType.StartArray:
                        switch (context.State)
                        {
                            case State.Initial:
                                context.State = context.NextState;
                                break;
                            case State.Items:
                                context.State = State.Source;
                                break;
                        }

                        break;
                    case JsonTokenType.EndArray:
                        switch (context.State)
                        {
                            case State.Items:
                            case State.Track:
                                context.State = State.Initial;
                                break;
                            case State.Destination:
                                context.State = State.Items;
                                break;
                        }
                        break;
                    case JsonTokenType.PropertyName:
                        if (context.State == State.Initial)
                        {
                            Encoding.UTF8.GetChars(json.ValueSpan, propertyBuffer);
                            switch (propertyBuffer[0])
                            {
                                case 'i':
                                    context.NextState = State.Items;
                                    break;
                                case 't':
                                    context.NextState = State.Track;
                                    break;
                            }
                        }

                        break;
                    case JsonTokenType.Number:
                        var value = json.GetInt32();
                        switch (context.State)
                        {
                            case State.Source:
                                context.CurrentSource = value;
                                context.State = State.Destination;
                                break;
                            case State.Destination:
                                context.Problems[context.CurrentProblemIndex++] = (context.CurrentSource, value);
                                break;
                            case State.Track:
                                context.Track[context.CurrentTrackIndex++] = value;
                                break;
                        }
                        break;
                }
            }

            state = json.CurrentState;

            return json.Position;
        }

        private static async Task<Context> Execute(PipeReader reader)
        {
            var context = new Context();
            try
            {
                var state = new JsonReaderState();
                while (true)
                {
                    var result = await reader.ReadAsync();

                    var position = result.Buffer.Start;
                    try
                    {
                        position = ReadNext(context, in result, ref state);

                        if (result.IsCompleted)
                            break;
                    }
                    finally
                    {
                        reader.AdvanceTo(position);
                    }
                }
            }
            finally
            {
                await reader.CompleteAsync();
            }

            return context;
        }

        [HttpPost("/microchallenge")]
        public async Task Solve()
        {
            try
            {
                HttpContext.Response.StatusCode = 200;
                HttpContext.Response.Headers.Add("Content-Type", "application/json");
                await HttpContext.Response.StartAsync();

                await using var writer = new Utf8JsonWriter(HttpContext.Response.Body);

                using var context = await Execute(HttpContext.Request.BodyReader);

                var results = new int[context.CurrentProblemIndex];
                Parallel.For(0, results.Length, index =>
                {
                    var (source, destination) = context.Problems[index];
                    int total = 0;
                    if (source < destination)
                    {
                        for (var j = source; j < destination; ++j)
                            total += context.Track[j];
                    }
                    else if (source > destination)
                    {
                        for (var j = source - 1; j >= destination; --j)
                            total += context.Track[j];
                    }

                    results[index] = total;
                });

                writer.WriteStartArray();
                foreach (var result in results)
                    writer.WriteNumberValue(result);
                writer.WriteEndArray();
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString());
            }
        }
    }
}