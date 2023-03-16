// See https://aka.ms/new-console-template for more information
using MemoryMappedRPC;
using System.Diagnostics;

System.Threading.Thread.Sleep(1000);

Console.WriteLine("Hello, World!");

Client client = new Client();
var pOptions = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
Parallel.For(0, 1000000, i =>
{
    var sw = Stopwatch.StartNew();
    var result = client.Call<int>("RPC_01", i);
    sw.Stop();
    //Console.WriteLine($"{i} returned {result} took {sw.ElapsedMilliseconds}");
});