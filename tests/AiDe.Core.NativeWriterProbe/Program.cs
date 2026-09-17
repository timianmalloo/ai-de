using System.Text.Json;
using AiDe.Core.Watcher;

if (args.Length != 2) return 2;
var writer = new CoordContractWriter(args[0], new Epoch());
var result = writer.Prepare("heartbeat", args[1]);
Console.WriteLine(JsonSerializer.Serialize(new { status = result.Status.ToString(), result.Code }));
if (result.Prepared is not { } prepared) return 3;
if (Console.ReadLine() != "append") return 4;
result = writer.Append(prepared);
Console.WriteLine(JsonSerializer.Serialize(new
{
    status = result.Status.ToString(), result.Code, result.Admission,
}));
return 0;

sealed class Epoch : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
}
