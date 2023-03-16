// See https://aka.ms/new-console-template for more information
using MemoryMappedRPC;

Console.WriteLine("Hello, World!");
Server server = new Server();

server.Subscribe((int i) => { return i + 1; }, "RPC_01");

Console.ReadLine();