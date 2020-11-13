using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

/// <summary>
/// Network related utility methods.
/// </summary>
internal static partial class NetworkUtil
{
    /// <summary>
    /// Ports that are known to have been handed out and not released by this application domain.
    /// tracking because WCF ports are sometimes reserved but do not show up in the system TCP tables.
    /// </summary>
    private static HashSet<int> knownUsedPorts = new HashSet<int>();

    /// <summary>
    /// Shared memory coordinator between different appdomain/process/terminial/users.
    /// </summary>
    private static ProcessValueCoordinator coordinatePorts = new ProcessValueCoordinator("LocalServerPort");

    /// <summary>Gets an unused port to be used, syncronized across the machine including appdomain/process/terminal/user.</summary>
    /// <returns>An unused port number.</returns>
    public static int GetUnusedLocalServerPort()
    {
        return coordinatePorts.Operate(GetUnusedLocalServerPort);
    }

    /// <summary>Removes <paramref name="portNumber"/> from tracking.</summary>
    /// <param name="portNumber">A previously used port number.</param>
    /// <returns>true if that port number was being tracked.</returns>
    public static bool ReleaseLocalServerPort(int portNumber)
    {
        return knownUsedPorts.Remove(portNumber);
    }

    /// <summary>Gets an unused port to be used.</summary>
    /// <param name="interopValue">The previously stored value in shared memory</param>
    /// <returns>A new port to be used.</returns>
    private static int GetUnusedLocalServerPort(int interopValue)
    {
        int uniqueLocalServerPort = (knownUsedPorts.Count == 0) ? 4000 : (knownUsedPorts.Max() + 1);
        if ((0 != interopValue) && !knownUsedPorts.Contains(interopValue) && (uniqueLocalServerPort <= interopValue))
        {
            // interopValue has been used
            // this appDomain has not put the interopValue as port into use
            // another process/appdomain has put it into use and it was larger than set of knownUsedPorts
            uniqueLocalServerPort = interopValue + 1;
        }

        HashSet<int> usedPorts = new HashSet<int>(knownUsedPorts);
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
        foreach (var info in properties.GetActiveTcpConnections())
        {
            usedPorts.Add(info.LocalEndPoint.Port);
            usedPorts.Add(info.RemoteEndPoint.Port);
            if (info.LocalEndPoint.Port >= uniqueLocalServerPort)
            {
                uniqueLocalServerPort = info.LocalEndPoint.Port + 1;
            }

            if (info.RemoteEndPoint.Port >= uniqueLocalServerPort)
            {
                uniqueLocalServerPort = info.RemoteEndPoint.Port + 1;
            }
        }

        foreach (IPEndPoint activeEndPoint in properties.GetActiveTcpListeners())
        {
            usedPorts.Add(activeEndPoint.Port);
            if (activeEndPoint.Port >= uniqueLocalServerPort)
            {
                uniqueLocalServerPort = activeEndPoint.Port + 1;
            }
        }

    TryAnotherPort:
        if (uniqueLocalServerPort > IPEndPoint.MaxPort)
        {
            for (int i = 1024; i < IPEndPoint.MaxPort; i++)
            {
                if (!usedPorts.Contains(i))
                {
                    uniqueLocalServerPort = i;
                    break;
                }
            }

            if (uniqueLocalServerPort > IPEndPoint.MaxPort)
            {
                throw new InvalidOperationException("Unable to find an available port between " +
                    IPEndPoint.MinPort + " and " + IPEndPoint.MaxPort + ". Consider ignoring " +
                    "remote end points that are known to be local in CreateUniqueLocalServerPort.");
            }
        }

        // Try the port first. The above APIs only work on IPv4. Currently there's no support for IPv6 versions
        //   in .NET. We could make native calls (code is rather hard) or use "netstat" (which would be really slow).
        //   For now just catching exceptions is probably the best.
        using (HttpListener listener = new HttpListener())
        {
            try
            {
                listener.Prefixes.Add("http://localhost:" + uniqueLocalServerPort.ToString() + "/");
                listener.Start();
            }
            catch (HttpListenerException listenerException)
            {
                // 32 means - Used by another process
                if (listenerException.ErrorCode != 32)
                {
                    throw;
                }

                usedPorts.Add(uniqueLocalServerPort);
                uniqueLocalServerPort = IPEndPoint.MaxPort + 1;
                goto TryAnotherPort;
            }
        }

        knownUsedPorts.Add(uniqueLocalServerPort);
        return uniqueLocalServerPort;
    }
}
