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

using System;
using System.IO;

namespace Functions
{
    public class Functions
    {
        //
        // Check whether specific phrases occur or not
        //
        static bool Occurs(string[] strs, string str, int offs)
        {
            for (int i = 0; i < strs.Length - offs; i++)
                if (strs[offs + i] == str)
                    return true;
            return false;
        }

        //
        // Set up a timestamp to the console for logging purposes
        //
        public const string dateFormat = "dd.MM.yyyy";
        public const string timeFormat = "HH:mm:ss";

        //
        // Log all actions and information
        //
        public static void LogToConsole(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToString(dateFormat)} {DateTime.Now.ToString(timeFormat)}] {message}");
        }

        //
        // Send a raw packet
        //
        public static void SendRawPacket(StreamWriter writer, string packet)
        {
            LogToConsole($"[SEND] {packet}");

            writer.WriteLine(packet);
            writer.Flush();
        }

        //
        // Send a message to either a channel or a user
        //
        public static void SendPrivmsg(StreamWriter writer, string to, string message)
        {
            SendRawPacket(writer, $"PRIVMSG {to} :{message}");
        }

        //
        // Send a notice to either a channel or a user
        //
        public static void SendNotice(StreamWriter writer, string to, string message)
        {
            SendRawPacket(writer, $"NOTICE {to} :{message}");
        }

        //
        // Compose a CTCP reponse, to be replied to a user
        //
        public static string CTCPResponseString(string message)
        {
            return (char)1 + message + (char)1;
        }
    }
}
