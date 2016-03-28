#region Copyright (c) 2010 Active Web Solutions Ltd
//
// (C) Copyright 2010 Active Web Solutions Ltd
//      All rights reserved.
//
// This software is provided "as is" without warranty of any kind,
// express or implied, including but not limited to warranties as to
// quality and fitness for a particular purpose. Active Web Solutions Ltd
// does not support the Software, nor does it warrant that the Software
// will meet your requirements or that the operation of the Software will
// be uninterrupted or error free or that any defects will be
// corrected. Nothing in this statement is intended to limit or exclude
// any liability for personal injury or death caused by the negligence of
// Active Web Solutions Ltd, its employees, contractors or agents.
//
#endregion

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Command.Shell;
using System.Threading;
using System.Net.Sockets;

namespace TelnetD
{
    class TelnetServer
    {
        public TcpClient tcpClient;
        public static NetworkStream networkStream;
        public static List<byte> ProviderBuf;
        private string password;
		static bool firstconnect = false;


        // 1-->For Enabling TCPKeepAlive,30 secs interval,1 sec duration after sending tcpkeepalive
        byte[] inValue = new byte[] { 1, 0, 0, 0, 48, 117, 0, 0, 1, 0, 0, 0 };
        static byte[] byte1 = new byte[] { 63,63,31,63,63,32,63,63,24,63,63,39,63,63,1,63,63,3,63,63,3 };
        byte[] outvalue = new byte[10];

        public TelnetServer(TcpClient tcpClient, string password)
        {
            this.tcpClient = tcpClient;
            this.tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            this.tcpClient.Client.IOControl(IOControlCode.KeepAliveValues, inValue, outvalue);

            networkStream = tcpClient.GetStream();
            this.password = password;
        }


        public void Banner()
        {
            StreamWriter streamWriter = new StreamWriter(networkStream);

            streamWriter.WriteLine("{0} {1}", Environment.OSVersion, Environment.MachineName);
            streamWriter.Write("Press Enter");
            streamWriter.Flush();
        }
        
        //--Clear screen
        public static void Clearscreen() //Clear Full Screen
        {
        	byte[] message = Encoding.ASCII.GetBytes("\u001B[1J\u001B[H");
        	//networkStream.Write(new byte[] { 0x1b, (byte)'[', (byte)'2', (byte)'J' }, 0, 4);
            networkStream.Write(message, 0, 4);
            //<ESC>[{ROW};{COLUMN}H \033[<L>;<C>H
            //networkStream.WriteByte();
           // \033[J2
            //networkStream.Flush();
            //Console.WriteLine(networkStream.Position.CompareTo//);
            set_cursor_loc(0,0);
        }
		//Setting the cursor position        
        public static void set_cursor_loc(int x, int y)
        {
            List<byte> buf_loc = Encoding.GetEncoding("Big5").GetBytes("[" + x + ";" + y + "H").ToList();
            buf_loc.Insert(0, 0x1b);
            networkStream.Write(buf_loc.ToArray(), 0, buf_loc.Count);
        }        
		//Clear from the cursor position to the end of the line
        public void clear_cursor2LineEnd()
        {
            networkStream.Write(new byte[] { 0x1b, (byte)'[', (byte)'K' }, 0, 3);
            networkStream.Flush(); 
        }
        
        public clinet_keyinput get_input_op()
        {
            byte[] buf = new byte[4];
            clinet_keyinput ck = new clinet_keyinput();
            int i = networkStream.Read(buf, 0, 4);

            ck.count = i;

            if (i == 1)
            {
                ck.SKeyTyep = SpecialKeyType.NULL;
                ck.key = (char)buf[0];

                if (buf[0] == 0x7f || buf[0] == 0x08)
                    ck.key = '\b';
            }

            if (i == 4 || i == 3) 
            {
                if (buf[0] == 0x1b && buf[1] == 0x4f && buf[2] == 0x41)
                    ck.SKeyTyep = SpecialKeyType.Up;
                else if (buf[0] == 0x1b && buf[1] == 0x4f && buf[2] == 0x42)
                    ck.SKeyTyep = SpecialKeyType.Down;
                else if (buf[0] == 0x1b && buf[1] == 0x4f && buf[2] == 0x44)
                    ck.SKeyTyep = SpecialKeyType.Left;
                else if (buf[0] == 0x1b && buf[1] == 0x4f && buf[2] == 0x43)
                    ck.SKeyTyep = SpecialKeyType.Right;
                else if (buf[0] == 0x1b && buf[1] == 0x5b && buf[2] == 0x35 && buf[3] == 0x7e)
                    ck.SKeyTyep = SpecialKeyType.PageUp;
                else if (buf[0] == 0x1b && buf[1] == 0x5b && buf[2] == 0x36 && buf[3] == 0x7e)
                    ck.SKeyTyep = SpecialKeyType.PageUp;

            }
            return ck;
        }        

    
        public static void Command(string cmd)
        {
            StreamWriter streamWriter = new StreamWriter(networkStream);

			streamWriter.WriteLine(cmd);
            streamWriter.Flush();
        }
        
        public static void Write(string cmd)
        {
            StreamWriter streamWriter = new StreamWriter(networkStream);

//            streamWriter.WriteLine("#: {0}",cmd);
			streamWriter.Write(cmd);
            streamWriter.Flush();

        }
        //--Screen output
        public static void ask_no_clinet_echo()//Asked not to enter the native side of the display
        {
            networkStream.Write(new byte[] { 0xff, 251, 31, 3, 1 }, 0, 5);
            networkStream.ReadByte(); 
            networkStream.ReadByte(); 
            networkStream.ReadByte();
        }       

        			private struct Codes {
				/* TELNET commands */
				public const byte GA = 249;
				public const byte WILL = 251;
				public const byte WONT = 252;
				public const byte DO = 253;
				public const byte DONT = 254;
				public const byte IAC = 255;
				public const byte NAWS = 31;    

				/* TELNET options */
				public const byte ECHO = 1;
				public const byte SUPP = 3;
			}
        
			public static int Negotiate(byte[] Buffer, int Count) {
				int resplen = 0;
				int index = 0;

				while (index < Count) {
					if (Buffer[index] == Codes.IAC) {
						try {
							switch (Buffer[index + 1]) {
								/* If two IACs are together they represent one data byte 255 */
							case Codes.IAC:
								{
									Buffer[resplen++] = Buffer[index];

									index += 2;

									break;
								}

								/* Ignore the Go-Ahead command */
							case Codes.GA:
								{
									index += 2;

									break;
								}

								/* Respond WONT to all DOs and DONTs */
							case Codes.DO:
							case Codes.DONT:
								{
									Buffer[index + 1] = Codes.WONT;

									lock(networkStream) {
										networkStream.Write(Buffer, index, 3);
									}

									index += 3;

									break;
								}

								/* Respond DONT to all WONTs */
							case Codes.WONT:
								{
									Buffer[index + 1] = Codes.DONT;

									lock(networkStream) {
										networkStream.Write(Buffer, index, 3);
									}

									index += 3;

									break;
								}

								/* Respond DO to WILL ECHO and WILL SUPPRESS GO-AHEAD */
								/* Respond DONT to all other WILLs                    */
							case Codes.WILL:
								{
									byte action = Codes.DONT;

									if (Buffer[index + 2] == Codes.ECHO) {
										action = Codes.DO;
									} else if (Buffer[index + 2] == Codes.SUPP) {
										action = Codes.DO;
									}

									Buffer[index + 1] = action;

									lock(networkStream) {
										networkStream.Write(Buffer, index, 3);
									}

									index += 3;

									break;
								}
							}
						} catch(IndexOutOfRangeException) {
							/* If there aren't enough bytes to form a command, terminate the loop */
							index = Count;
						}
					} else {
						if (Buffer[index] != 0) {
							Buffer[resplen++] = Buffer[index];
						}

						index++;
					}
				}

				return (resplen);
			}
        
		static int IndexOfByte( byte[] input, byte[] pattern )
			{
			if (pattern.Length <= 0)
				return -1;
			
			 byte firstByte = pattern[0];
			 int index = -1;
			
			 if ( ( index = Array.IndexOf( input, firstByte ) ) >= 0 )
			 {
			  for(int i=0; i<pattern.Length; i++)
			  {
			   if (index+i>=input.Length || pattern[ i ]!=input[index+i]) 
			  		return -1;
			  }
			 }
			 return index;
			}	
		
    public static bool SendData(byte[] data)
    {
        networkStream.Write(data, 0, data.Length);
        return true;
    }
    
   /* public int ReceiveData()
    {
        bytesReceived = networkStream.Read(bufferReceived, 0, 8192);

        return bytesReceived;
    }*/
		
        private static bool SendCommand(int negotiation, int option)
        {
            byte[] sendData;
            sendData = TelnetHelper.GetCommand(negotiation, option);
            return SendData(sendData);
        }
        
        private bool SendSubNegotiation(int option, byte[] data)
        {
            byte[] SubNegotiationCommand;
            SubNegotiationCommand = TelnetHelper.GetSubNegotiationCommand(option, data);

            return SendData(SubNegotiationCommand);
        }        
        
        private void DisplayReceivedData(byte[] data, int dataLength)
        {
            for (int i = 0; i < dataLength; ++i)
            {
                byte dataByte = data[i];

                if (dataByte != 0)
                {
                    if (dataByte == TelnetHelper.SB)
                        Console.Write("SB:   ");
                    else if (dataByte == TelnetHelper.SE)
                        Console.Write("SE:    ");
                    else if (dataByte == TelnetHelper.WILL)
                        Console.Write("WILL:  ");
                    else if (dataByte == TelnetHelper.WONT)
                        Console.Write("WON'T: ");
                    else if (dataByte == TelnetHelper.DO)
                        Console.Write("DO:    ");
                    else if (dataByte == TelnetHelper.DONT)
                        Console.Write("DON'T: ");
                    else if (dataByte == TelnetHelper.IAC)
                        Console.Write("\nIAC  ");
                    else if (dataByte == TelnetHelper.ESC)
                    {
                        Console.Write("\nESC: ");
                    }
                    else // Get Option Description of Command
                        Console.Write(TelnetHelper.GetOptionDescription(dataByte) + " "); //dataByte.ToString("X")
                }
            }

            Console.WriteLine();
        }
        
         public static void SendMsg(byte[] ba) {
        	Console.WriteLine("SendMsg2");
                      lock (ProviderBuf) {
                    int ct = ProviderBuf.Count();
                    if (ct > 0) {
                        for (int wx = 0; wx < ct; wx++) {
                      //      SendBuffer[wx] = ProviderBuf[wx];
                        }
                        ProviderBuf.Clear();
                    }
                    //Sender.SetBuffer(SendBuffer, 0, ct);
                }  
        }
        
        // TODO: Right now we're just messing around with the negotiations
        public static void Negotiate1() {
            Console.WriteLine("Negotiating...");
            List<byte> lb = new List<byte>();
            lb.Add(Codes.IAC);
            lb.Add(Codes.DO);
            lb.Add(Codes.NAWS);
            lb.Add(Codes.IAC);
            lb.Add(Codes.WILL);
            lb.Add(Codes.ECHO);
            //SendMsg(lb.ToArray());  
            
                foreach (byte b in lb.ToArray()) {
				//ProviderBuf.Add(b);
				//networkStream.WriteByte(b);
                }
            
            //Console.WriteLine();
            
        }
        
        public static string ReadLine()
        {
        	StreamReader streamReader = new StreamReader(networkStream);
        	string text = string.Empty;
        	try {
        		text = streamReader.ReadLine();
        		//networkStream.Read

				//return streamReader.ReadLine();
				//byte[] message = Encoding.ASCII.GetBytes(t);
				//networkStream.Write(new byte[] { 0x1b, (byte)'[', (byte)'2', (byte)'J' }, 0, 4);
				//networkStream.Write(message, 0, 4);^[OM ^[[2J
				//networkStream.Write(new byte[] { 0x1b, (byte)'[', (byte)'O', (byte)'M' }, 0, 4);
				//networkStream.Write(new byte[2] { 13, 10 },0,2);
				if (firstconnect) {
					
					Console.WriteLine(text);
					string lastWord = text.Split(' ').Last();
					
					byte[] byte2 = Encoding.ASCII.GetBytes(text); 
					string hex = BitConverter.ToString(byte2).Replace("-", string.Empty);	
					Console.WriteLine(hex);					
            		byte[] newbyte = byte2.Except(byte1).ToArray();
            		text = Encoding.UTF8.GetString(newbyte);
            	

					// Loop through contents of the array.
					/*foreach (byte element in byte1)
	{
	    Console.WriteLine("{0} = {1}", element, (char)element);
	}*/
					Console.WriteLine(text);
				}
        	} catch (Exception ex) {
        		Console.WriteLine(ex.Message);
        		Console.WriteLine(ex.Source);
        		//throw;
        	}
        	//return 
        	firstconnect = false;
        	return text;
        }  
        
        public static void Prompt()
        {
            StreamWriter streamWriter = new StreamWriter(networkStream);

            streamWriter.Write(Environment.MachineName +"# ");
            streamWriter.Flush();

        }  
        public bool Login()
        {
            StreamWriter streamWriter = new StreamWriter(networkStream);
            StreamReader streamReader = new StreamReader(networkStream);

            for (int tries = 0; tries < 3; tries++)
            {
                streamWriter.Write("Password: ");
                streamWriter.Flush();
                string response = streamReader.ReadLine();
                if (response == password)
                {
                    streamWriter.WriteLine("\n");
                    streamWriter.Flush();
                    return true;
                }
            }

            return false;
        }

      
        public void Connect()
        {
        	StreamWriter streamWriter = new StreamWriter(networkStream);
        	StreamReader streamReader = new StreamReader(networkStream);
        	var input = new InputCommand();
            var display = new Display();
            try
            {
				firstconnect = true;
				Negotiate1();
			//networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.DO, TelnetHelper.SUPP },0,3);
			//networkStream.Write(new byte[] { 0xff, 251, 1 }, 0, 3);
			//networkStream.Write(new byte[] { 0xfd, 251, 31, 3, 1 }, 0, 5);
			/*networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.WILL, 31},0,3);
			networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.WILL, 32},0,3);
			networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.WILL, 24},0,3);
			networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.WILL, 39},0,3);
			networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.DO, Codes.ECHO } ,0,3);
			networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.WILL, Codes.SUPP},0,3);
			networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.DO, Codes.SUPP},0,3);
			*/
			//networkStream.Write(new byte[]{ TelnetHelper.IAC, TelnetHelper.SB, 24, 1, TelnetHelper.IAC, TelnetHelper.SE },0,6);
			//networkStream.Write(new byte[]{ TelnetHelper.DO, 0x18 },0,2);
			//networkStream.Write(new byte[]{ TelnetHelper.DO, 0xFD },0,2);
			// Things our client is requesting
            /*SendCommand(TelnetHelper.DO, 0x03); // DO Suppress Go Ahead0xFF 0xFD 0x03
IAC|WILL|NAWS
IAC|WILL|32
IAC|WILL|24
IAC|WILL|39
IAC|DO|ECHO
IAC|WILL|SUPPRESS_GOAHEAD
IAC|DO|SUPPRESS_GOAHEAD
ID 1 now has scrsz 117x23
IAC|SB|NAWS|0|117|0|23|IAC|SE|

            SendCommand(TelnetHelper.DO, 0xFF); 
            SendCommand(TelnetHelper.DO, 0xFD);
			SendCommand(TelnetHelper.DO, 0x18);*/

				/*networkStream.Write(new byte[]{ TelnetHelper.DO, 0x03 },0,2);
				networkStream.Write(new byte[]{ TelnetHelper.DO, 0xFF },0,2);
				networkStream.Write(new byte[]{ TelnetHelper.DO, 0xFD },0,2);
				networkStream.Write(new byte[]{ TelnetHelper.DO, 0x18 },0,2);*/
                //Banner();
               // ReadLine();
                /*if ((password != null) && !Login())
                {
                    // Login failed
                    tcpClient.Close();
                    return;
                }*/
                //Clearscreen();
				//Prompt();

                byte[] buffer = new byte[256];
                int i;
                   /* while ((i = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                    	var buf = Encoding.Unicode.GetString(buffer,0,i);
                    	
                    	Console.WriteLine(buf);
					}*/
                   if ((i = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                   	Console.WriteLine(i);
                   
				//Negotiate(buffer, buffer.Length);

                //const int CONTROL_D = 4;

                try
                {
                	ShellProgram.Start(input, display);
                	/*
                    int i;
                    while ((i = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                    	                	
                    var buf = Encoding.Default.GetString(buffer,0,i);
					Console.WriteLine(buf);
					if (string.IsNullOrWhiteSpace(buf)){
						Prompt();
					}
					if (buf.Contains("clear")){
					Clearscreen();
					clear_cursor2LineEnd();
					}
                 if (buf.Contains("test")){
                    	Console.WriteLine("got InvalidTimeZoneException");                    	
                Command("Basic Shell Commands - try `help full` for all commands");
                Command("=======================================================");
                	
                }
                    	//Console.WriteLine(buf);
                    	//Console.WriteLine(buffer[0]);
                        if ((i > 0) && (buffer[0] == CONTROL_D))
                        {
                            outputThread.Abort();
                            errorThread.Abort();
                            //process.Close();
                            //process.Kill();
                            tcpClient.Close();

                            Console.WriteLine("Connection {0} closed", Thread.CurrentThread.ManagedThreadId);

                            return;
                        }
                        //process.StandardInput.Write(Encoding.ASCII.GetChars(buffer), 0, i);
                    }*/
                }
                catch (Exception ex)
                {
                    // Clean up
                    //outputThread.Abort();
                    //errorThread.Abort();
                    tcpClient.Close();
                    Console.WriteLine("Clean up on Connection {0}", Thread.CurrentThread.ManagedThreadId);
                    //throw;
                    Console.WriteLine(ex.Message);
                }

            }
            catch (Exception ex)
            {
                // Catch any errors on this thread, print them out, and shut down the thread so any 
                // other concurrent threads can continue without the process dying.
                Console.WriteLine("Error on Connection {0}", Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
