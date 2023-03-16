namespace MemoryMappedRPC
{
    internal class Message
    {
        public string ReplyTo { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
