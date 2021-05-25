/*
IRCrobot/2.0
Copyright (C) 2020-2021  Léanne M. Wolf & Stefan Ljungstrand

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#nullable enable

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using static Settings.Settings;
using static Functions.Functions;

namespace IRCrobot
{
    class IRC
    {
        static void Main(string[] args)
        {
            string version = "2.0-beta";

            //
            // Establish a connection
            //
            using (var client = new TcpClient())
            {
                LogToConsole($"Connecting to [{serv}:{(ssl ? "+" : "")}{port}] ...");
                client.Connect(serv, port);

                Stream baseStream = client.GetStream(), stream;
                SslStream sslStream;

                //
                // Perform SSL handshake
                // Is SSL is disabled in the settings: Use plaintext
                //
                if (ssl)
                {
                    LogToConsole($"Performing SSL handshake with [{serv}:+{port}] ...");
                    sslStream = new SslStream(baseStream);
                    sslStream.AuthenticateAsClient(serv);
                    stream = sslStream;
                }
                else
                {
                    stream = baseStream;
                }

                //
                // Connect to IRC
                //
                using (var writer = new StreamWriter(stream))
                using (var reader = new StreamReader(stream))
                {
                    //
                    // Send required user information
                    //
                    SendRawPacket(writer, $"USER {ident} 0 * :{ident}");
                    SendRawPacket(writer, $"NICK {nick}");
                    if (pass != null) SendRawPacket(writer, $"PRIVMSG NickServ :IDENTIFY {user} {pass}");

                    //
                    // Set up a timer to proactively send a PING packet every 30 seconds.
                    // Some networks will fail to send a PING packet after a length of time,
                    // and this makes IRCrobot time out and disconnect.
                    //
                    Timer _pingTimer = new Timer((e) =>
                    {
                        SendRawPacket(writer, $"PING :{nick}");
                    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

                    int hello_count = 0;

                    //
                    // Commands and behavior while IRCrobot is running
                    //
                    while (client.Connected)
                    {
                        var data = reader.ReadLine();
                        //
                        // Check for PING requests sent by the server (or anyone else) and respond accordingly.
                        // This prevents IRCrobot from disconnecting due to inactivity or unexpected TCP connection exits.
                        //
                        if (data != null)
                        {
                            var d = data.Split(' ');
                            LogToConsole($"[RECV] {data}");

                            if (d[0] == "PING")
                            {
                                SendRawPacket(writer, $"PONG {d[1]}");
                            }
                            if (d.Length > 1)
                            {
                                switch (d[1])
                                {
                                    //
                                    // If the intended nickname is already in use, try another one
                                    //
                                    case "433":
                                        {
                                            nick = nick + "_";
                                            SendRawPacket(writer, $"NICK {nick}");
                                            // TODO : Don't repeat this code snippet
                                            if (pass != null) SendRawPacket(writer, $"PRIVMSG NickServ :IDENTIFY {user} {pass}");
                                            break;
                                        }
                                    //
                                    // Join channel
                                    //
                                    case "376":
                                    case "422":
                                        {
                                            SendRawPacket(writer, $"JOIN {chan}");
                                            break;
                                        }
                                    case "PRIVMSG":
                                        {
                                            string[] _splitOnColon = data.Split(':', 3);

                                            string message = _splitOnColon[2];

                                            string[] _splitOnExclamation = _splitOnColon[1].Split("!");

                                            string from = _splitOnExclamation[0];
                                            string receiver = d[2];  // Either ourselves, or a channel
                                            string respond_to = receiver != nick ? receiver : from;

                                            //
                                            // Respond to CTCP
                                            //
                                            if (message.StartsWith((char)1) && message.EndsWith((char)1))
                                            {
                                                //
                                                // This is a CTCP packet
                                                //
                                                string ctcp_message = message.Trim((char)1);

                                                //
                                                // We always need to send a CTCP response to the person, not the channel
                                                // The only exception is for if you want to deal with ACTION (/me)
                                                //
                                                string ctcp_from = from;

                                                //
                                                // Separate command name from possible remainder
                                                //
                                                string[] ctcp_msg_parts = ctcp_message.Split(" ", 2);
                                                string ctcp_command = ctcp_msg_parts[0];
                                                string? ctcp_rest = ctcp_msg_parts.Length <= 1 ? null : ctcp_msg_parts[1];

                                                //
                                                // Each response sends the same CTCP command, as a NOTICE, with reply as remainder after command
                                                //
                                                switch (ctcp_command.ToUpper())
                                                {
                                                    case "CLIENTINFO":
                                                        SendNotice(writer, ctcp_from, CTCPResponseString("CLIENTINFO CLIENTINFO VERSION PING TIME"));
                                                        break;
                                                    case "VERSION":
                                                        SendNotice(writer, ctcp_from, CTCPResponseString($"VERSION IRCrobot/{version}"));
                                                        break;
                                                    case "PING":
                                                        SendNotice(writer, ctcp_from, CTCPResponseString("PING" + (ctcp_rest == null ? "" : " " + ctcp_rest)));
                                                        break;
                                                    case "TIME":
                                                        SendNotice(writer, ctcp_from, CTCPResponseString("TIME " + DateTime.UtcNow.ToString("s")));
                                                        break;
                                                }

                                            }

                                            //
                                            // Display the version and the copyright
                                            //
                                            if (d.Length > 3)
                                            {
                                                if (d[3] == ":!version")
                                                {
                                                    var sender = data.Split('!')[0].Substring(1);
                                                    string platform = OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "macOS" : OperatingSystem.IsLinux() ? "Linux" : "Unknown platform";
                                                    SendPrivmsg(writer, respond_to, $"IRCrobot/{version} on {platform}");
                                                }
                                            }

                                            //
                                            // Display the uptime of IRCrobot
                                            //
                                            if (d.Length > 3)
                                            {
                                                if (d[3] == ":!uptime")
                                                {
                                                    var sender = data.Split('!')[0].Substring(1);
                                                    var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                                                    var seconds = Math.Truncate(uptime.TotalSeconds);
                                                    TimeSpan ts = TimeSpan.FromSeconds(seconds);
                                                    string output = ts.Days != 0
                                                                  ? string.Format("{0:D1} days, {1:D1} hours, {2:D1} minutes and {3:D1} seconds", ts.Days, ts.Hours, ts.Minutes, ts.Seconds)
                                                                  : ts.Hours != 0
                                                                  ? string.Format("{0:D1} hours, {1:D1} minutes and {2:D1} seconds", ts.Hours, ts.Minutes, ts.Seconds)
                                                                  : ts.Minutes != 0
                                                                  ? string.Format("{0:D1} minutes and {1:D1} seconds", ts.Minutes, ts.Seconds)
                                                                  : ts.Seconds != 0
                                                                  ? string.Format("{0:D1} seconds", ts.Seconds)
                                                                  : "less than a second";
                                                    SendPrivmsg(writer, respond_to, $"{nick} is up for {output}.");

                                                }
                                            }

                                            //
                                            // Copycat ACTION
                                            //
                                            if (message.StartsWith("ACTION ") && message.EndsWith((char)1) && new Random().Next(6) == 0)
                                            {
                                                string[] actions = message.Trim((char)1).Split(" ", 2);
                                                SendPrivmsg(writer, respond_to, CTCPResponseString($"ACTION {actions[1]} too"));
                                            }

                                            // **************************************************
                                            // START :: Your own commands should belong here
                                            // **************************************************

                                            //
                                            // Example command
                                            //
                                            if (d.Length > 3)
                                            {
                                                if (d[3] == ":!example")
                                                {
                                                    var random = new Random();
                                                    var output = new List<string> {
                                                        "This is a randomized output (1)",
                                                        "This is a randomized output (2)",
                                                        "This is a randomized output (3)"
                                                    };
                                                    int index = random.Next(output.Count);
                                                    var sender = data.Split('!')[0].Substring(1);
                                                    SendPrivmsg(writer, respond_to, output[index]);
                                                }
                                            }

                                            //
                                            // Example command
                                            //
                                            if (d.Length > 3)
                                            {
                                                if (d[3] == ":!example2")
                                                {
                                                    var sender = data.Split('!')[0].Substring(1);
                                                    SendPrivmsg(writer, respond_to, $"This is {nick}!");
                                                }
                                            }

                                            //
                                            // Example command
                                            //
                                            if (d.Length > 3)
                                            {
                                                if (d[3].ToLower() == ":!blubb" || d[3].ToLower() == ":!plnpp" || d[3].ToLower() == ":!gnapp")
                                                {
                                                    var random = new Random();
                                                    var sender = data.Split('!')[0].Substring(1);
                                                    var output = new List<string> {
                                                         "Blubb !",
                                                         "Plnpp !",
                                                         "Gnapp !"
                                                    };
                                                    SendPrivmsg(writer, respond_to, output[random.Next(output.Count)]);
                                                }
                                            }

                                            // **************************************************
                                            // END :: Your own commands should belong here
                                            // **************************************************
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
