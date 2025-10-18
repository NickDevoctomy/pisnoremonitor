using System;

namespace PiSnoreMonitor.Services
{
    public class PooledBlock
    {
        public byte[] Buffer = [];
        public int Count;
    }
}