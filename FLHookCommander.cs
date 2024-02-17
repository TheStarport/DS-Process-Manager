using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DSProcessManager
{
    /// <summary>
    /// Allows commands to be sent to flhook command socket.
    /// Note: Only ASCII charnames are supported. There seems to be a bug in
    /// FLHook's unicode socket processing. 
    /// </summary>
    public class FLHookSocket : IDisposable
    {
        /// <summary>
        /// If execute command fails then this contains the error description.
        /// </summary>
        public string LastCmdError = "";

        /// <summary>
        /// IP address of the flhook command socket
        /// </summary>
        public string server;

        /// <summary>
        /// Port number of the flhook command socket
        /// </summary>
        private int port;

        /// <summary>
        /// Port number of the flhook command socket
        /// </summary>
        private bool unicode;

        /// <summary>
        /// Login code of the flhook command socket
        /// </summary>
        string login;

        /// <summary>
        /// The connection to the flhook command socket.
        /// </summary>
        TcpClient cmdSocket = null;

        /// <summary>
        /// The stream for the flhook command socket
        /// </summary>
        NetworkStream cmdStream = null;

        /// True if the socket is connected and logged into FLHook.
        bool isLoggedIn = false;

        /// <summary>
        /// Setup a flhook command instance in command and event mode.
        /// </summary>
        public FLHookSocket()
        {
            CheckSettings();
        }

        ~FLHookSocket()
        {
            Dispose();
        }

        public void Dispose()
        {
            CloseCmdSocket();
        }

        /// <summary>
        /// Check the socket connection and settings. If necessary tear down 
        /// the existing and create a new connection. THis is a synchronise function
        /// and may run for a long time before returning.
        /// </summary>
        /// <returns>true if the socket is ready for commands otherwise false</returns>
        private bool CheckSettings()
        {
            bool changed = false;
            lock (AppSettings.Default)
            {
                // Check for settings changes.
                if (server != AppSettings.Default.setFLHookIP)
                    changed = true;
                if (port != Convert.ToInt16(AppSettings.Default.setFLHookPort))
                    changed = true;
                if (login != AppSettings.Default.setFLHookPassword)
                    changed = true;
                if (unicode != AppSettings.Default.setFLHookUnicode)
                    changed = true;

                // Update to current settings.
                server = AppSettings.Default.setFLHookIP;
                port = Convert.ToInt32(AppSettings.Default.setFLHookPort);
                login = AppSettings.Default.setFLHookPassword;
                unicode = AppSettings.Default.setFLHookUnicode;
            }

            // If the settings have changed then cache the new settings and
            // break the existing socket
            if (changed)
            {
                CloseCmdSocket();
            }

            // The socket is okay. No action required
            if (cmdSocket != null && cmdStream != null)
                return true;

            // If the port is 0 then don't start flhook.
            if (port == 0)
            {
                LastCmdError = "FLHook comms are disabled.";
                return false;
            }

            // Start a connection and start reading.
            try
            {
                stReplyBuf = "";
                cmdSocket = new TcpClient(server, port);
                cmdSocket.ReceiveTimeout = 5000;
                cmdStream = cmdSocket.GetStream();
            
                // Wait for the welcome message.
                string reply = ReceiveReply(cmdStream, unicode);
                if (reply != "Welcome to FLHack, please authenticate")
                    throw new Exception("no login message '" + reply + "'");

                // Send the pass and wait for OK
                SendCommand(cmdStream, String.Format("pass {0}", login), unicode);
                reply = ReceiveReply(cmdStream, unicode);
                if (reply != "OK")
                    throw new Exception("no pass ok message '" + reply + "'");

                isLoggedIn = true;
            }
            catch (Exception e)
            {
                LastCmdError = e.Message;
                CloseCmdSocket();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Close the command socket.
        /// </summary>
        private void CloseCmdSocket()
        {
            isLoggedIn = false;
            if (cmdStream != null)
            {
                try { cmdStream.Close(); }
                catch (Exception ex) { LastCmdError = ex.Message; }
                finally { cmdStream = null; }
            }

            if (cmdSocket != null)
            {
                try { cmdSocket.Close(); }
                catch (Exception ex) { LastCmdError = ex.Message; }
                finally { cmdSocket = null; }
            }
        }

        /// <summary>
        /// Return true if this socket is connected and logged in.
        /// </summary>
        public bool IsConnected()
        {
            if (cmdSocket == null)
                return false;

            if (!cmdSocket.Connected)
                return false;

            return isLoggedIn;
        }

        /// <summary>
        /// Check that comms with flhook are working.
        /// </summary>
        /// <returns>True if successful otherwise false.</returns>
        public bool CmdTest()
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand("test");
        }

        /// <summary>
        /// Request server info from flhook.
        /// </summary>
        /// <returns>True if successful otherwise false.</returns>
        public bool CmdServerInfo(out int load, out bool npcSpawnEnabled, string upTime)
        {
            LastCmdError = "";

            load = 0;
            npcSpawnEnabled = false;
            upTime = "<unknown>";

            if (!CheckSettings())
                return false;

            try
            {
                SendCommand(cmdStream, "serverinfo", unicode);
                LastCmdError = ReceiveReply(cmdStream, unicode);
                if (!LastCmdError.StartsWith("serverload"))
                    return false;
                ReceiveReply(cmdStream, unicode); // Eat up the OK.

                string[] keys;
                string[] values;
                ParseLine(LastCmdError, out keys, out values);

                int result;
                if (int.TryParse(values[0], out result))
                    load = result;

                if (keys[1] != "npcspawn")
                    return false;
                if (values[1] == "enabled")
                    npcSpawnEnabled = true;

                if (keys[2] != "uptime")
                    return false;
                upTime = values[2];
                return true;
            }
            catch (Exception ex)
            {
                LastCmdError = ex.Message;
                CloseCmdSocket();
            }
            return false;
        }

        public struct PlayerInfo
        {
            public string charname;
            public int id;
            public string ip;
            public int ping;
            public string system;
        }

        /// <summary>
        /// Request server info from flhook.
        /// </summary>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdGetPlayers(Dictionary<int, PlayerInfo> playerInfoList)
        {
            LastCmdError = "";

            playerInfoList.Clear();

            if (!CheckSettings())
                return false;

            try
            {
                SendCommand(cmdStream, "getplayers", unicode);
                while (true)
                {
                    string reply = ReceiveReply(cmdStream, unicode);
                    if (reply.StartsWith("ERR"))
                    {
                        LastCmdError = reply;
                        return false;
                    }
                    else if (reply.StartsWith("OK"))
                    {
                        LastCmdError = "OK";
                        return true;
                    }
                    else
                    {
                        string[] keys;
                        string[] values;
                        ParseLine(reply, out keys, out values);

                        // Message arrives in response to playerinfo command.
                        // charname=? clientid=? ip=? host=? ping=? base=? system=?
                        PlayerInfo info = new PlayerInfo();
                        info.charname = values[0];
                        info.id = Convert.ToInt32(values[1]);
                        info.ip = values[2];
                        info.ping = Convert.ToInt32(values[4]);
                        info.system = values[6];
                        playerInfoList.Add(info.id, info);

                    }
                }
            }
            catch (Exception ex)
            {
                LastCmdError = ex.Message;
                CloseCmdSocket();
            }
            return false;
        }


        /// <summary>
        /// Rename a character. This operation blocks until complete and may 
        /// take several seconds to do.
        /// </summary>
        /// <param name="oldName">The old name.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdRename(string oldName, string newName)
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand(String.Format("rename {0} {1}", oldName, newName));
        }


        /// <summary>
        /// Request that FLServer save the character. This operation blocks until
        /// complete and may take several seconds to do.
        /// </summary>
        /// <param name="charName">The charname.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdSaveChar(string charName)
        {
            if (!CheckSettings())
                return false;

            if (CmdIsOnServer(charName))
                return ExecuteCommand(String.Format("savechar {0}", charName));
            return false;
        }

        /// <summary>
        /// Ban a character. This operation blocks until complete and may 
        /// take several seconds to do. This bans the whole accounts.
        /// </summary>
        /// <param name="charName">The charname.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdBan(string charName)
        {
            if (!CheckSettings())
                return false;

            if (CmdIsOnServer(charName))
                ExecuteCommand(String.Format("kick {0}", charName));
            return ExecuteCommand(String.Format("ban {0}", charName));
        }

        /// <summary>
        /// Ban a character. This operation blocks until complete and may 
        /// take several seconds to do. This bans the whole accounts.
        /// </summary>
        /// <param name="id">The player id.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdKickBan(int id)
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand(String.Format("kickban$ {0}", id));
        }

        /// <summary>
        /// Unban a character. This operation blocks until complete and may 
        /// take several seconds to do. This unbans the whole account.
        /// </summary>
        /// <param name="oldName">The charname.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdUnban(string charName)
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand(String.Format("unban {0}", charName));
        }

        /// <summary>
        /// Delete a character. This operation blocks until complete and may 
        /// take several seconds to do.
        /// </summary>
        /// <param name="oldName">The charname.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdDeleteChar(string charName)
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand(String.Format("deletechar {0}", charName));
        }

        /// <summary>
        /// Send a universe wide message
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdMsgU(string msg)
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand(String.Format("msgu {0}", msg));
        }

        /// <summary>
        /// Kick a character. This operation blocks until complete and may 
        /// take several seconds to do. This bans the whole accounts.
        /// </summary>
        /// <param name="oldName">The charname.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdKick(string charName)
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand(String.Format("kick {0}", charName));
        }

        /// <summary>
        /// Kick a character. This operation blocks until complete and may 
        /// take several seconds to do. This bans the whole accounts.
        /// </summary>
        /// <param name="id">The player id.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdKick(int id)
        {
            if (!CheckSettings())
                return false;
            return ExecuteCommand(String.Format("kick$ {0}", id));
        }

        /// <summary>
        /// Check if character is on server.
        /// </summary>
        /// <param name="oldName">The charname.</param>
        /// <returns>True if successfull otherwise false.</returns>
        public bool CmdIsOnServer(string charName)
        {
            if (!CheckSettings())
                return false;
            bool onserver = false;
            try
            {
                SendCommand(cmdStream, String.Format("isonserver {0}", charName), unicode);
                while (true)
                {
                    string reply = ReceiveReply(cmdStream, unicode);
                    if (reply.StartsWith("ERR"))
                    {
                        LastCmdError = reply;
                        return false;
                    }
                    else if (reply.StartsWith("onserver=no"))
                    {
                        onserver = false;
                    }
                    else if (reply.StartsWith("OK"))
                    {
                        LastCmdError = "OK";
                        return onserver;
                    }
                    else if (reply.StartsWith("onserver=yes"))
                    {
                        onserver = true;
                    }
                }
            }
            catch (Exception e)
            {
                LastCmdError = e.Message;
                CloseCmdSocket();
                return false;
            }
        }

        static public void ParseLine(string line, out string[] keys, out string[] values)
        {
            string[] items = line.Split(new char[] { ' ' });
            keys = new string[items.Length];
            values = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                int valueStartIndex = items[i].IndexOf('=');
                if (valueStartIndex >= 0)
                {
                    keys[i] = items[i].Substring(0, valueStartIndex);
                    values[i] = items[i].Substring(valueStartIndex + 1);
                }
                else
                {
                    keys[i] = items[i];
                    values[i] = "";
                }
            }
        }

        /// <summary>
        /// Open a socket to flhook, login and execute the specified
        /// command. On failure LastExecuteError is set to the error reason.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>True on success otherwise false.</returns>
        private bool ExecuteCommand(string command)
        {
            LastCmdError = "";

            if (cmdSocket == null)
            {
                LastCmdError = "No connection to flhook";
                return false;
            }

            try
            {
                SendCommand(cmdStream, command, unicode);
                LastCmdError = ReceiveReply(cmdStream, unicode);
                if (LastCmdError != "OK")
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                // Console.WriteLine("Error '" + ex.Message + "' when executing flhook command '" + command + "'", "");
                LastCmdError = ex.Message;
                CloseCmdSocket();
            }
            return false;
        }

        /// <summary>
        /// Send a command to the network stream to flhook.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="command">The command to send.</param>
        private void SendCommand(NetworkStream stream, string command, bool unicode)
        {
            byte[] txBuf;
            if (unicode)
                txBuf = Encoding.Unicode.GetBytes(command + "\n");
            else
                txBuf = Encoding.ASCII.GetBytes(command + "\n"); 
            stream.Write(txBuf, 0, txBuf.Length);
        }

        /// <summary>
        /// The receive buffer.
        /// </summary>
        string stReplyBuf = "";

        /// <summary>
        /// Waits for a reply line from flhook. Horrible socket polling
        /// but meh, it works.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The reply</returns>
        private string ReceiveReply(NetworkStream stream, bool unicode)
        {
            // Receive the response.
            byte[] rxBuf = new byte[1024];


            DateTime abortReadTime = DateTime.Now.AddSeconds(5);
            while (true)
            {
                // If we've found a line break then return the line.
                int endOfLine = stReplyBuf.IndexOf("\n");
                if (endOfLine >= 0)
                {
                    char[] delims = { '\n', '\r' };
                    string reply = stReplyBuf.Substring(0, endOfLine).TrimEnd(delims);
                    stReplyBuf = stReplyBuf.Remove(0, endOfLine + 1);
                    //Console.WriteLine(reply);
                    return reply;
                }

                // Stop waiting for a reply if we have waited too long.
                if (DateTime.Now > abortReadTime)
                    return null;

                // Sleep if no data is available.
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // Otherwise read some bytes from the stream.
                int rxBytes = stream.Read(rxBuf, 0, rxBuf.Length);
                if (unicode)
                    stReplyBuf += System.Text.Encoding.Unicode.GetString(rxBuf, 0, rxBytes);
                else
                    stReplyBuf += System.Text.Encoding.ASCII.GetString(rxBuf, 0, rxBytes);
            }

        }
    }

    class FLHookEventSocket : IDisposable
    {
        /// If true then this socket will enter event mode
        FLHookListener eventListener = null;

        /// <summary>
        /// Thread for receiving event mode data.
        /// </summary>
        Thread receiveEventThread = null;

        /// <summary>
        /// Thread for checker the configuration and resetting
        /// the socket if necessary\.
        /// </summary>
        Thread cfgCheckerThread = null;

        /// <summary>
        /// The logging interface to report diagnostic messages on.
        /// </summary>
        LogRecorderInterface log = null;

        /// <summary>
        /// The receive buffer.
        /// </summary>
        string stReplyBuf = "";

        /// <summary>
        /// If true write debug log entries
        /// </summary>
        decimal debug = 0;

        /// <summary>
        /// The connection to FLHook
        /// </summary>
        TcpClient rxSocket = null;

        /// <summary>
        /// The stream to read/write on the socket.
        /// </summary>
        NetworkStream rxStream = null;

        /// <summary>
        /// Synchronisation object.
        /// </summary>
        Object locker = new Object();

        /// <summary>
        /// Start the log
        /// </summary>
        /// <param name="eventListener"></param>
        public FLHookEventSocket(FLHookListener eventListener, LogRecorderInterface log)
        {
            this.eventListener = eventListener;
            this.log = log;
            if (this.eventListener != null)
            {
                receiveEventThread = new Thread(new ThreadStart(ReceiveEventThread));
                receiveEventThread.Name = "FLEventReceive";
                receiveEventThread.IsBackground = true;
                receiveEventThread.Start();

                cfgCheckerThread = new Thread(new ThreadStart(ConfigurationCheckerThread));
                cfgCheckerThread.Name = "FLEventReceiveCfgChecker";
                cfgCheckerThread.IsBackground = true;
                cfgCheckerThread.Start();
            }
        }

        ~FLHookEventSocket()
        {
            Dispose();
        }

        /// <summary>
        /// Shutdown the socket.
        /// </summary>
        public void Dispose()
        {
            eventListener = null;

            if (cfgCheckerThread != null)
            {
                cfgCheckerThread.Abort();
                cfgCheckerThread = null;
            }

            // Close the socket to terminate any blocking reads,
            // terminate the thread and wait for it to shutdown.
            if (receiveEventThread != null)
            {
                lock (locker)
                {
                    CloseSocket();
                    receiveEventThread.Abort();
                }
                receiveEventThread = null;
            }
        }

        /// <summary>
        /// Close the socket;
        /// </summary>
        private void CloseSocket()
        {
            lock (locker)
            {
                if (rxSocket != null)
                {
                    rxSocket.Close();
                    rxSocket = null;
                }
            }
        }

        /// <summary>
        /// Send a command to the network stream to flhook.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="command">The command to send.</param>
        private void SendCommand(NetworkStream stream, string command, bool unicode)
        {
            byte[] txBuf;
            if (unicode)
                txBuf = Encoding.Unicode.GetBytes(command + "\n");
            else
                txBuf = Encoding.ASCII.GetBytes(command + "\n");
            if (debug > 1) log.AddLog("flhook tx: " + command.Trim());
            stream.Write(txBuf, 0, txBuf.Length);
        }

        /// <summary>
        /// Waits for a reply line from flhook. Horrible socket polling
        /// but meh, it works.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The reply</returns>
        private string ReceiveReply(NetworkStream stream, bool unicode)
        {
            // Receive the response.
            byte[] rxBuf = new byte[1024];

            DateTime abortReadTime = DateTime.Now.AddSeconds(5);
            while (true)
            {
                // If we've found a line break then return the line.
                int endOfLine = stReplyBuf.IndexOf("\n");
                if (endOfLine >= 0)
                {
                    char[] delims = { '\n', '\r' };
                    string reply = stReplyBuf.Substring(0, endOfLine).TrimEnd(delims);
                    stReplyBuf = stReplyBuf.Remove(0, endOfLine + 1);
                    return reply;
                }

                // Stop waiting for a reply if we have waited too long.
                if (DateTime.Now > abortReadTime)
                    return null;

                // Otherwise read some bytes from the stream.
                int rxBytes = stream.Read(rxBuf, 0, rxBuf.Length);
                if (rxBytes > 0)
                {
                    string reply;
                    if (unicode)
                        reply = System.Text.Encoding.Unicode.GetString(rxBuf, 0, rxBytes);
                    else
                        reply = System.Text.Encoding.ASCII.GetString(rxBuf, 0, rxBytes);
                    if (debug > 1) log.AddLog("flhook rx: " + reply.Trim());
                    stReplyBuf += reply;
                }
            }
        }

        /// <summary>
        /// Thread for checking the configuration and resetting
        /// the socket if necessary.
        /// </summary>
        private void ConfigurationCheckerThread()
        {
            string server = "";
            int port = 0;
            string login = "";
            bool unicode = false;

            while (true)
            {
                lock (AppSettings.Default)
                {
                    debug = AppSettings.Default.setDebug;

                    // Check for settings changes.
                    bool changed = false;
                    if (server != AppSettings.Default.setFLHookIP)
                        changed = true;
                    else if (port != Convert.ToInt32(AppSettings.Default.setFLHookPort))
                        changed = true;
                    else if (login != AppSettings.Default.setFLHookPassword)
                        changed = true;
                    else if (unicode != AppSettings.Default.setFLHookUnicode)
                        changed = true;

                    // Notify the receiver to reset it's configuration
                    if (changed && receiveEventThread != null)
                    {
                        server = AppSettings.Default.setFLHookIP;
                        port = Convert.ToInt32(AppSettings.Default.setFLHookPort);
                        login = AppSettings.Default.setFLHookPassword;
                        unicode = AppSettings.Default.setFLHookUnicode;
                        debug = AppSettings.Default.setDebug;

                        // Close the socket to terminate any blocking read and
                        // interrupt the thread.
                        lock (locker)
                        {
                            receiveEventThread.Interrupt();
                            CloseSocket();
                        }
                    }
                }

                Thread.Sleep(10000);
            }
        }

        /// <summary>
        /// The event receiver thread. This thread never exits.
        /// </summary>
        private void ReceiveEventThread()
        {
            string server = "";
            int port = 0;
            string login = "";
            bool unicode = false;
            while (true)
            {
                try
                {
                    Thread.Sleep(10000);

                    lock (AppSettings.Default)
                    {
                        server = AppSettings.Default.setFLHookIP;
                        port = Convert.ToInt32(AppSettings.Default.setFLHookPort);
                        login = AppSettings.Default.setFLHookPassword;
                        unicode = AppSettings.Default.setFLHookUnicode;
                        debug = AppSettings.Default.setDebug;
                    }

                    // Do nothing if flhook is disabled.
                    if (port == 0)
                        break;

                    // Establish the socket connection
                    stReplyBuf = "";

                    lock (locker)
                    {
                        CloseSocket();
                        rxSocket = new TcpClient(server, port);
                        rxSocket.ReceiveTimeout = 5000;
                        rxStream = rxSocket.GetStream();
                    }

                    if (debug > 0) log.AddLog("flhook: opened connection");

                    // Wait for the welcome message.
                    if (ReceiveReply(rxStream, unicode) != "Welcome to FLHack, please authenticate")
                        throw new Exception("no login message");

                    // Send the pass and wait for OK
                    SendCommand(rxStream, String.Format("pass {0}", login), unicode);
                    if (ReceiveReply(rxStream, unicode) != "OK")
                        throw new Exception("no pass ok message");

                    // ASk hook to enter event mode.
                    SendCommand(rxStream, "eventmode", unicode);
                    if (ReceiveReply(rxStream, unicode) != "OK")
                        throw new Exception("no eventmode ok message");

                    if (debug > 0) log.AddLog("flhook: login complete");

                    // Loop receiving events
                    byte[] rxBuf = new byte[1000];
                    rxSocket.ReceiveTimeout = 0;

                    // Otherwise read some bytes from the stream.
                    string reply = ReceiveReply(rxStream, unicode);
                    while (reply != null)
                    {
                        string[] keys;
                        string[] values;
                        FLHookSocket.ParseLine(reply, out keys, out values);
                        eventListener.ReceiveFLHookEvent(keys[0], keys, values, reply);

                        reply = ReceiveReply(rxSocket.GetStream(), unicode);
                    }
                }
                catch (ThreadAbortException)
                {
                    if (debug > 0) log.AddLog("flhook: shutdown connection");
                }
                catch (ThreadInterruptedException)
                {
                    if (debug > 0) log.AddLog("flhook: configuration reset");
                }
                catch (Exception ex)
                {
                    if (debug > 0) log.AddLog("flhook: '" + ex.Message + "'");
                }
                finally
                {
                    if (debug > 0) log.AddLog("flhook: closing connection");
                    stReplyBuf = "";
                    CloseSocket();
                }
            }
        }
    }
}