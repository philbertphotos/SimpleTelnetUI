using System;

namespace TelnetD
{
    public static class TelnetHelper
    {
        // Telnet Negotiation Commands
        public const byte SE = 0xF0; // subnegotiation end
        public const byte SB = 0xFA; // subnegotiation start
        public const byte WILL = 0xFB;
        public const byte WONT = 0xFC;
        public const byte DO = 0xFD;
        public const byte DONT = 0xFE;
        public const byte IAC = 0xFF;
        public const byte SUPP = 0x03;

        public const int TERMINAL_COLS = 80; // terminal width in characters
        public const int TERMINAL_ROWS = 24; // terminal height in characters

        // Telnet Data Commands
        public const byte ESC  = 0x1B;

        static string[] TelnetOptions =
        {
            // http://www.iana.org/assignments/telnet-options
            // TELNET OPTIONS

            // (last updated 2003-11-06)

            // The Telnet Protocol has a number of options that may be negotiated.
            // These options are listed here.  "Internet Official Protocol Standards"
            // (STD 1) provides more detailed information.
             
            "Binary Transmission",  // 00
            "Echo",                 // 01
            "Reconnection",         // ...
            "Suppress Go Ahead",
            "Approx Message Size Negotiation",
            "Status",
            "Timing Mark",
            "Remote Controlled Trans and Echo",
            "Output Line Width",
            "Output Page Size",
            "Output Carriage-Return Disposition",
            "Output Horizontal Tab Stops",
            "Output Horizontal Tab Disposition",
            "Output Formfeed Disposition",
            "Output Vertical Tabstops",
            "Output Vertical Tab Disposition",
            "Output Linefeed Disposition",
            "Extended ASCII",
            "Logout",
            "Byte Macro",
            "Data Entry Terminal",
            "SUPDUP",
            "SUPDUP Output",
            "Send Location",
            "Terminal Type",
            "End of Record",
            "TACACS User Identification",
            "Output Marking",
            "Terminal Location Number",
            "Telnet270 Regime",
            "X.3 PAD",
            "Negotiate About Window Size",
            "Terminal Speed",
            "Remote Flow Control",
            "Linemode",
            "X Display Location",
            "Environment Option",
            "Authentication Option",
            "Encryption Option",
            "New Environment Option",
            "TN3270E",
            "XAUTH",
            "CHARSET",
            "Telnet Remote Serial Port (RSP)",
            "Com Port Control Option",
            "Telnet Suppress Local Echo",
            "Telnet Start TLS",
            "KERMIT",
            "SEND-URL",
            "FORWARD_X"                 // 49
        };

        // Get the value of the option description
        public static int GetOptionCode(string optionDescription)
        {
            for (int i = 0; i < 50; ++i)
            {
                if (TelnetOptions[i] == optionDescription)
                    return i;
            }

            return -1;
        }

        // Get the description of option
        public static string GetOptionDescription(int option)
        {
            if ( option >= 0 && option <= 49)
                return TelnetOptions[option];
            else if (option >= 50 && option <= 137)
                return "Unassigned";
            else if (option == 138)
                return "TELOPT PRAGMA LOGON";
            else if (option == 139)
                return "TELOPT SSPI LOGON";
            else if (option == 140)
                return "TELOPT PRAGMA HEARTBEAT";
            else if (option == 255)
                return "Extended-Options-List";
            else
                return "ERROR (" + option.ToString("X") + ")";
        }

        // Get command in format of IAC NEGOTIATION OPTION in form of byte array
        public static byte[] GetCommand(int negotiation, int option)
        {
            byte[] sendData = { 0xFF, 0x00, 0x00 };

            if (negotiation >= 0xFA && negotiation <= 0xFE)
                sendData[1] = (byte)negotiation;
            else
                return null;

            if (option >= 0 && option <= 49)
                sendData[2] = (byte)option;
            else
                return null;

            return sendData;
        }

        public static byte[] GetSubNegotiationCommand(int option, byte[] data)
        {
            //C: IAC SB TERMINAL TYPE IS terminaltype IAC SE
            //   FF  FA 1F               ??           FF  F0

            byte[] buffer = new byte[ data.Length + 5 ];

            if (option < 0 || option > 49)
                return null;

            buffer[0] = IAC; // Command
            buffer[1] = SB;   // Subnegotiation Start
            buffer[2] = (byte)option; // Passed in telnet option

            // Copy data into our return buffer
            for (int i = 0; i < data.Length; ++i)
                buffer[3 + i] = data[i];

            buffer[4 + data.Length - 1] = IAC; // Command
            buffer[5 + data.Length - 1] = SE; // Subnegotiation End

            return buffer;
        }
    }
}
