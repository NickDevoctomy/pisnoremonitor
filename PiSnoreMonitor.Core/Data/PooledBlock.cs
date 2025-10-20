using System;

namespace PiSnoreMonitor.Data
{
    public class PooledBlock
    {
        public byte[] Buffer = [];
        public int Count;
    }
}