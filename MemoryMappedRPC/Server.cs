using System.IO.MemoryMappedFiles;
using System.Text.Json;

namespace MemoryMappedRPC
{
    public class Server
    {
        public void Subscribe(Delegate del, string alias)
        {
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    MemoryMappedFile.OpenExisting(alias);
                }
                catch (Exception)
                {
                    MemoryMappedFile.CreateNew(alias, 1024);
                }

                //Create memory, mutex and semaphore for RPC method.
                if (!Mutex.TryOpenExisting($"{alias}_Mutex", out Mutex mutex))
                    mutex = new Mutex(false, $"{alias}_Mutex");

                if (!Semaphore.TryOpenExisting($"{alias}_Semaphore", out Semaphore semaphore))
                    semaphore = new Semaphore(0, 1, $"{alias}_Semaphore");

                while (true)
                {
                    //Wait for method to be called.
                    semaphore.WaitOne();

                    //Somebody else got the mutex first.
                    if (!mutex.WaitOne(0))
                        continue;

                    string message = string.Empty;

                    using (var mmf = MemoryMappedFile.OpenExisting(alias))
                    {
                        // Read the string from the shared memory region
                        using (var reader = new StreamReader(mmf.CreateViewStream()))
                        {
                            message = reader.ReadToEnd().Replace("\0", string.Empty);
                        }
                    }

                    mutex.ReleaseMutex();

                    //Deserialize
                    Message m = null;
                    object[] args = null;

                    try
                    {
                        m = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(message);
                        args = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(m.Body, new JsonInt32Converter());
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    //Console.WriteLine($"{alias} recieved message {message}");

                    //Invoke
                    object? result = del.DynamicInvoke(args);

                    //Serialize
                    string stringResult = Newtonsoft.Json.JsonConvert.SerializeObject(result);                    
                    string s = JsonSerializer.Serialize(new Message()
                    {
                        Body = stringResult,
                        ReplyTo = string.Empty
                    });

                    //reply on reply mapped memory.
                    using var replySemaphore = Semaphore.OpenExisting($"{m.ReplyTo}_Semaphore");
                    using (var mmf = MemoryMappedFile.OpenExisting(m.ReplyTo))
                    {
                        // Read the string from the shared memory region
                        using (var writer = new StreamWriter(mmf.CreateViewStream()))
                        {
                            writer.Write(s);
                        }
                    }
                    replySemaphore.Release();
                }
            });
        }
    }
}