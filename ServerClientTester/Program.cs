// See https://aka.ms/new-console-template for more information
using MemoryMappedRPC;

Console.WriteLine("Hello, World!");

Server server = new Server();

Client client = new Client();
client.Call("RPC_01");