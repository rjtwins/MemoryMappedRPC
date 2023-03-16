using System.IO.MemoryMappedFiles;

namespace MemoryMappedRPC
{
    public class Client
    {
        public T Call<T>(string alias, params object[] args)
        {
            string body = Newtonsoft.Json.JsonConvert.SerializeObject(args);
            //Message
            Message m = new Message()
            {
                Body = body,
                ReplyTo = Guid.NewGuid().ToString()
            };
            string s = System.Text.Json.JsonSerializer.Serialize(m);

            //Reply memory
            using var replySemaphore = new Semaphore(0, 1, $"{m.ReplyTo}_Semaphore");
            using var replymmf = MemoryMappedFile.CreateNew(m.ReplyTo, 1024);

            using var mutex = Mutex.OpenExisting($"{alias}_Mutex");

            //Wait for memory to be available.
            mutex.WaitOne();

            //Console.WriteLine(s);

            //Open remote methods memory
            using var semaphore = Semaphore.OpenExisting($"{alias}_Semaphore");
            using (var mmf = MemoryMappedFile.OpenExisting($"{alias}"))
            {
                // Read the string from the shared memory region
                using (var writer = new StreamWriter(mmf.CreateViewStream()))
                {
                    writer.Write(s);
                }
            }

            //Signal remote message memory is was written to.
            semaphore.Release();

            //Release memory.
            mutex.ReleaseMutex();

            replySemaphore.WaitOne();

            // Read the string from the shared memory region
            string message = string.Empty;
            using (var reader = new StreamReader(replymmf.CreateViewStream()))
            {
                message = reader.ReadToEnd().Replace("\0", string.Empty);
            }
            
            Message result = System.Text.Json.JsonSerializer.Deserialize<Message>(message);
            T bodyObject = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result.Body, new JsonInt32Converter());

            return bodyObject;
        }

        //public void Call(string alias, params object[] args)
        //{
        //    int i = (int)args[0];

        //    //Message
        //    Message m = new Message()
        //    {
        //        Body = i.ToString(),
        //        ReplyTo = Guid.NewGuid().ToString()
        //    };
        //    string s = System.Text.Json.JsonSerializer.Serialize(m);

        //    //Reply memory
        //    using var replySemaphore = new Semaphore(0, 1, $"{m.ReplyTo}_Semaphore");
        //    using var replymmf = MemoryMappedFile.CreateNew(m.ReplyTo, 1024);

        //    using var mutex = Mutex.OpenExisting($"{alias}_Mutex");

        //    //Wait for memory to be available.
        //    mutex.WaitOne();

        //    //Open remote methods memory
        //    using var semaphore = Semaphore.OpenExisting($"{alias}_Semaphore");
        //    using (var mmf = MemoryMappedFile.CreateOrOpen($"{alias}", 1024))
        //    {
        //        // Read the string from the shared memory region
        //        using (var writer = new StreamWriter(mmf.CreateViewStream()))
        //        {
        //            writer.Write(s);
        //        }
        //    }

        //    //Release memory.
        //    mutex.ReleaseMutex();

        //    //Signal remote message memory is was written to.
        //    semaphore.Release();

        //    replySemaphore.WaitOne();
        //    // Read the string from the shared memory region
        //    using (var reader = new StreamReader(replymmf.CreateViewStream()))
        //    {
        //        string message = reader.ReadToEnd();
        //        Console.WriteLine(message); // Output: Hello, world!
        //    }
        //}
    }
}
