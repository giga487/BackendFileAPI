namespace APIFileServer.source
{
    public class RestAPIFileCache
    {
        public class MemoryItem
        {
            public byte[]? MemoryDump { get; set; } = null;
            public DateTime DateTime { get; set; } = DateTime.Now;

            public MemoryItem(byte[] stream)
            { 
                MemoryDump = new byte[stream.Length];
                Array.Copy(stream, MemoryDump, stream.Length);
            }
        }

        private volatile Dictionary<string, MemoryItem> Memory = new Dictionary<string, MemoryItem>();
        public long MemorySize { get; private set; } = 0;
        public long MaxMemory { get; set; } = 1024*1024*1024;

        public RestAPIFileCache(): this(1024*1024*1024)
        {
        }

        public RestAPIFileCache(long MaxMemorySize)
        {
            MaxMemory = MaxMemorySize;
        }

        public bool AddMemory(string key, byte[] streamData)
        {
            long size = streamData.Length * sizeof(byte);

            while (size > MaxMemory - MemorySize || MemorySize < 0)
            {
                RemoveOldest();
            }

            try
            {
                if (Memory.TryAdd(key, new MemoryItem(streamData)))
                {
                    MemorySize += size;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool Get(string key, out byte[]? output)
        {
            if (Memory.TryGetValue(key, out var value))
            {
                output = value.MemoryDump;
                return true;
            }

            output = null;
            return false;
        }

        public void RemoveOldest()
        {
            if (Memory == null || Memory.Count == 0)
                return;

            var oldest = Memory.MinBy(x => x.Value.DateTime);
            Memory.Remove(oldest.Key);
        }

    }
}
