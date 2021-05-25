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

namespace Settings
{
    public class Settings
    {
        public const string serv = "irc.oftc.net";
        public const bool ssl = true;
        public const int port = ssl ? 6697 : 6667;
        public const string original_nick = "both";
        public static string nick = original_nick;
        public const string user = "both";
        public const string ident = "neither";
        public const string? pass = null;
        public const string chan = "#ircrobot";
        public const string dateFormat = "dd.MM.yyyy";
        public const string timeFormat = "HH:mm:ss";
    }
}
