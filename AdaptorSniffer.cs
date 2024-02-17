using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace DSProcessManager
{
    public class Adapter
    {
        NetworkInterface nic;
        IPAddress ip;

        public IPAddress IP { get { return ip; } }

        public Adapter(NetworkInterface nic, IPAddress ip) { this.nic = nic; this.ip = ip; }

        public override string ToString()
        {
            return "Socket: " + nic.Description + " (" + ip.ToString() + ")";
        }

        /// <summary>
        /// Return the list of network adaptors on this machine.
        /// </summary>
        /// <returns></returns>
        public static List<Adapter> GetAdapters()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            List<Adapter> adapters = new List<Adapter>(nics.Length);

            foreach (NetworkInterface nic in nics)
            {
                try
                {
                    foreach (UnicastIPAddressInformation unicastIpInfo in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastIpInfo.Address != null && !unicastIpInfo.Address.IsIPv6LinkLocal)
                        {
                            adapters.Add(new Adapter(nic, unicastIpInfo.Address));
                        }
                    }
                }
                catch { }
            }
            return adapters;
        }
    }

    public class Sniffer
    {
        List<AdapterSniffer> sniffers = new List<AdapterSniffer>();

        /// <summary>
        /// Start sniffing for packets on all interfaces.
        /// </summary>
        /// <returns></returns>
        public void StartSniffing()
        {
            foreach (Adapter adapter in Adapter.GetAdapters())
            {
                try
                {
                    AdapterSniffer sniffer = new AdapterSniffer(adapter);
                    sniffer.StartSniffing();
                    sniffers.Add(sniffer);
                }
                catch { }
            }
        }

        public class TrafficInformation
        {
            public long rxBytes10sec;
            public long rxBytes10min;
            public long txBytes10sec;
            public long txBytes10min;
        }

        /// <summary>
        /// Return a summary of data suitable for display.
        /// </summary>
        /// <returns></returns>
        public Dictionary<IPAddress, TrafficInformation> GetDataSummary()
        {
            long time10sOld = (long)(DateTime.Now - DateTime.MinValue).TotalSeconds - (10);
            Dictionary<IPAddress, TrafficInformation> summary = new Dictionary<IPAddress, TrafficInformation>();

            foreach (AdapterSniffer sniffer in sniffers)
            {
                lock (sniffer.data)
                {
                    foreach (IPAddress ip in sniffer.data.Keys)
                    {
                        TrafficInformation ti = new TrafficInformation();
                        SortedList<long, AdapterSniffer.BucketContents> ipData = sniffer.data[ip];
                        foreach (long dataTime in ipData.Keys)
                        {
                            AdapterSniffer.BucketContents bc = ipData[dataTime];
                            ti.rxBytes10min += bc.rxBytes;
                            ti.txBytes10min += bc.txBytes;
                            if (dataTime >= time10sOld)
                            {
                                ti.rxBytes10sec += bc.rxBytes;
                                ti.txBytes10sec += bc.txBytes;
                            }
                        }
                        summary[ip] = ti;
                    }
                }
            }

            return summary;
        }
    }

    public class AdapterSniffer
    {
        private System.Threading.Thread threadDataExpireRun;
        private IPAddress adapterIP = null;
        private Socket socket = null;
        private byte[] buffer = new byte[65535];
        private bool isActive = false;

        public AdapterSniffer(Adapter adapter)
        {
            adapterIP = adapter.IP;
            threadDataExpireRun = new System.Threading.Thread(ExpireOldDataRun);
            threadDataExpireRun.IsBackground = true;
            threadDataExpireRun.Start();
        }

        ~AdapterSniffer()
        {
            if (socket != null)
                socket.Close();
            if (threadDataExpireRun != null)
                threadDataExpireRun.Abort();
        }

        public void StartSniffing()
        {
            if (socket == null)
            {
                try
                {
                    if (adapterIP.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Raw, ProtocolType.Raw);
                        socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.HeaderIncluded, true);
                    }
                    else
                    {
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                    }

                    socket.Bind(new IPEndPoint(adapterIP, 0));
                    byte[] optionInValue = { 1, 0, 0, 0 };
                    socket.IOControl(IOControlCode.ReceiveAll, BitConverter.GetBytes((int)1), null);
                    
                    IAsyncResult sniffResult = socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.ReceivePacketListener), null);
                    isActive = true;
                }
                catch { socket = null; }
            }
        }

        public void StopSniffing()
        {
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
            isActive = false;
        }

        public class BucketContents
        {
            public long rxBytes;
            public long txBytes;
        }

        public Dictionary<IPAddress, SortedList<long, BucketContents>> data = new Dictionary<IPAddress, SortedList<long, BucketContents>>();

        /// <summary>
        /// Add the rxBytes count the current bucket for this IP address.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="rxBytes"></param>
        void RecordRxData(IPAddress ip, long rxBytes)
        {
            lock (data)
            {
                if (!data.ContainsKey(ip))
                    data[ip] = new SortedList<long, BucketContents>();

                long dataTime = (long)(DateTime.Now - DateTime.MinValue).TotalSeconds;
                if (!data[ip].ContainsKey(dataTime))
                    data[ip][dataTime] = new BucketContents();
                data[ip][dataTime].rxBytes += rxBytes;
            }
        }

        /// <summary>
        /// Add the txBytes count the current bucket for this IP address.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="rxBytes"></param>
        void RecordTxData(IPAddress ip, long txBytes)
        {
            lock (data)
            {
                if (!data.ContainsKey(ip))
                    data[ip] = new SortedList<long, BucketContents>();

                Int64 dataTime = (long)(DateTime.Now - DateTime.MinValue).TotalSeconds;
                if (!data[ip].ContainsKey(dataTime))
                    data[ip][dataTime] = new BucketContents();
                data[ip][dataTime].txBytes += txBytes;
            }
        }

        /// <summary>
        /// Remove any data that is older than 10 mins.
        /// </summary>
        void ExpireOldDataRun()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(10000);

                if (AppSettings.Default.setEnableNetMonitor && !isActive)
                    StartSniffing();
                else if (!AppSettings.Default.setEnableNetMonitor && isActive)
                    StopSniffing();

                long dataExpireTime = (long)(DateTime.Now - DateTime.MinValue).TotalSeconds - (40);
                List<IPAddress> expiredIPs = new List<IPAddress>();

                lock (data)
                {
                    foreach (IPAddress ip in data.Keys)
                    {
                        List<long> expiredTimes = new List<long>();
                        SortedList<long, BucketContents> ipData = data[ip];
                        foreach (long dataTime in ipData.Keys)
                        {
                            if (dataTime < dataExpireTime)
                            {
                                expiredTimes.Add(dataTime);
                            }
                        }
                        foreach (long dataTime in expiredTimes)
                        {
                            ipData.Remove(dataTime);
                        }
                        if (ipData.Count == 0)
                        {
                            expiredIPs.Add(ip);
                        }
                    }
                    foreach (IPAddress ip in expiredIPs)
                    {
                        data.Remove(ip);
                    }
                }
            }
        }

        private void ReceivePacketListener(IAsyncResult result)
        {
            try
            {
                int received = socket.EndReceive(result);
                if (received > 20)
                {
                    // Decode the packet if it is UDP
                    byte protocol = buffer[9];
                    if (protocol == 0x11)
                    {
                        // Source (offset=12)
                        byte[] srcBytes = new byte[4];
                        Array.Copy(buffer, 12, srcBytes, 0, srcBytes.Length);
                        IPAddress sourceIP = new System.Net.IPAddress(srcBytes);

                        // Destination (offset=16)
                        byte[] destBytes = new byte[4];
                        Array.Copy(buffer, 16, destBytes, 0, destBytes.Length);
                        IPAddress destIP = new System.Net.IPAddress(destBytes);

                        // Source port (offset=20)
                        int srcPort = (int)buffer[20] << 8 | (int)buffer[21];

                        // Destination port (offset=22)
                        int destPort = (int)buffer[22] << 8 | (int)buffer[23];
                        
                        // Inbound traffic
						if (!sourceIP.Equals(adapterIP) && destIP.Equals(adapterIP))
                        {
                            if (destPort >= AppSettings.Default.setNetLowPort
                                && destPort <= AppSettings.Default.setNetHighPort)
                            {
                                RecordRxData(sourceIP, received);
                            }
                        }
                        // Outbound traffic
						else if (sourceIP.Equals(adapterIP) && !destIP.Equals(adapterIP))
                        {
                            if (srcPort >= AppSettings.Default.setNetLowPort
                                && srcPort <= AppSettings.Default.setNetHighPort)
                            {
                                RecordTxData(destIP, received);
                            }
                        }
                    }
                }
            }
            catch { }
            try
            {
                if (isActive)
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.ReceivePacketListener), null);
            }
            catch { }
        }
    }
}
