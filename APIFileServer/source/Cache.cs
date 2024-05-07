namespace APIFileServer.source
{
    public class RestAPIFileCache
    {
        public class MemoryItem
        {
            public readonly int MinimumTimeInCache = 20;
            public byte[]? MemoryDump { get; set; } = null;
            public DateTime DateTime { get; set; } = DateTime.Now; 
            public DateTime ElapsedTime { get => DateTime.AddSeconds(MinimumTimeInCache); }
            public MemoryItem(byte[] stream)
            { 
                MemoryDump = new byte[stream.Length];
                Array.Copy(stream, MemoryDump, stream.Length);
            }
            
            public void Update()
            {
                DateTime  = DateTime.Now;
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
        
        public void UpdateAccess(string key)
        {
            if (Memory.TryGetValue(key, out var value))
            {
                value.Update();
            }
        }
        public List<string> Items => Memory.Keys.ToList();

        public bool AddMemory(string key, byte[] streamData)
        {
            long size = streamData.Length * sizeof(byte);

            while (size > MaxMemory - MemorySize || MemorySize < 0)
            {
                if (!RemoveOldest())
                {
                    return false; //allora devo fornire il dato senza cache
                }
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
                value.Update();
                return true;
            }

            output = null;
            return false;
        }

        public bool RemoveOldest()
        {
            if (Memory == null || Memory.Count == 0)
                return false;

            var oldest = Memory.MinBy(x => x.Value.DateTime);

            if (oldest.Value.ElapsedTime < DateTime.Now)
            {
                return false; // non posso eliminarla
            }
            MemorySize -= oldest.Value.MemoryDump.Length;

            Console.WriteLine($"Removed from cache {oldest.Key}");
            Memory.Remove(oldest.Key);

            return true;
        }

    }
}
