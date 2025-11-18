using System.Linq;

namespace RConsole.Editor
{
    public static class NETUtils
    {

        /// <summary>
        /// 获取所有 IPv4 地址
        /// </summary>
        /// <returns> 所有 IPv4 地址的数组 </returns>
        public static string[] GetIPv4Addresses()
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                        continue;
                    var ipProps = ni.GetIPProperties();
                    foreach (var ua in ipProps.UnicastAddresses)
                    {
                        var addr = ua.Address;
                        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !System.Net.IPAddress.IsLoopback(addr))
                        {
                            list.Add(addr.ToString());
                        }
                    }
                }
            }
            catch
            {
            }

            if (list.Count == 0) list.Add("127.0.0.1");
            return list.Distinct().ToArray();
        }
    }
}
