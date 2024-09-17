using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace NetworkUtil;

public static class Networking
{
    /////////////////////////////////////////////////////////////////////////////////////////
    // Server-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
    /// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
    /// AcceptNewClient will continue the event-loop.
    /// </summary>
    /// <param name="toCall">The method to call when a new connection is made</param>
    /// <param name="port">The the port to listen on</param>
    public static TcpListener StartServer(Action<SocketState> toCall, int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);  
        listener.Start();

        //Tuple of our toCall and our server instance, so we can pass it into the BeginAcceptSocket method. 
        var tuple = Tuple.Create(toCall, listener);

        //If we fail, make a broken state. 
        try
        {
            listener.BeginAcceptSocket(AcceptNewClient, tuple);
        }
        catch (Exception e)
        {
            SocketState brokenState = new(toCall, e.Message);
            brokenState.OnNetworkAction(brokenState);
        }

        return listener;
    }

    /// <summary>
    /// To be used as the callback for accepting a new client that was initiated by StartServer, and 
    /// continues an event-loop to accept additional clients.
    ///
    /// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
    /// OnNetworkAction should be set to the delegate that was passed to StartServer.
    /// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
    /// 
    /// If anything goes wrong during the connection process (such as the server being stopped externally), 
    /// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccurred flag set to true 
    /// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
    /// an error occurs.
    ///
    /// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
    /// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
    /// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
    private static void AcceptNewClient(IAsyncResult ar)
    {
        var states = (Tuple<Action<SocketState>, TcpListener>)ar.AsyncState!;

        //Unpacking the tuple, making a socket and socket state for using later.
        Action<SocketState> toCall = states.Item1;
        TcpListener listener = states.Item2;
        Socket s;
        SocketState state;

        // try-catch
        try
        {
            s = listener.EndAcceptSocket(ar);
            state = new SocketState(toCall, s);
        }
        catch(Exception e)
        {
            SocketState brokenState = new SocketState(toCall, "Failed to EndAcceptSockets " + e.Message);
            brokenState.OnNetworkAction(brokenState);
            return;
        }

        state.OnNetworkAction(state);

        // try-catch
        listener.BeginAcceptSocket(AcceptNewClient, states);
    }

    /// <summary>
    /// Stops the given TcpListener.
    /// </summary>
    public static void StopServer(TcpListener listener)
    {
        //If the server is already stopped, just return. That's basically all this does, but also encompasses similar issues.
        try
        {
            listener.Stop();
        }
        catch
        {
            return;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////
    // Client-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of connecting to a server via BeginConnect, 
    /// and using ConnectedCallback as the method to finalize the connection once it's made.
    /// 
    /// If anything goes wrong during the connection process, toCall should be invoked 
    /// with a new SocketState with its ErrorOccurred flag set to true and an appropriate message 
    /// placed in its ErrorMessage field. Depending on when the error occurs, this should happen either
    /// in this method or in ConnectedCallback.
    ///
    /// This connection process should timeout and produce an error (as discussed above) 
    /// if a connection can't be established within 3 seconds of starting BeginConnect.
    /// 
    /// </summary>
    /// <param name="toCall">The action to take once the connection is open or an error occurs</param>
    /// <param name="hostName">The server to connect to</param>
    /// <param name="port">The port on which the server is listening</param>
    public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
    {
        // Establish the remote endpoint for the socket.
        IPHostEntry ipHostInfo;
        IPAddress ipAddress = IPAddress.None;

        // Determine if the server address is a URL or an IP
        try
        {
            ipHostInfo = Dns.GetHostEntry(hostName);
            bool foundIPV4 = false;
            foreach (IPAddress addr in ipHostInfo.AddressList)
            {
                if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    foundIPV4 = true;
                    ipAddress = addr;
                    break;
                }
            }
            // Didn't find any IPV4 addresses, make a broken state if we didn't.
            if (!foundIPV4)
            {
                SocketState brokenState = new(toCall, "Failed to find IPv4 Address.");
                brokenState.OnNetworkAction(brokenState);

                return;
            }
        }
        catch (Exception)
        {
            // see if host name is a valid ipaddress, make a broken state if it isn't
            try
            {
                ipAddress = IPAddress.Parse(hostName);
            }
            catch (Exception e )
            {
                SocketState brokenState = new(toCall, "Failed to find IPv4 Address. " + e.Message);
                brokenState.OnNetworkAction(brokenState);

                return;
            }
        }

        // Create a TCP/IP socket.
        Socket s = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        SocketState state = new(toCall, s);

        // This disables Nagle's algorithm (google if curious!)
        // Nagle's algorithm can cause problems for a latency-sensitive 
        // game like ours will be 
        s.NoDelay = true;

        IAsyncResult result;

        try
        {
            result = s.BeginConnect(new IPEndPoint(ipAddress, port), ConnectedCallback, state);

        }
        //Makes a broken state if we can't connect.
        catch (Exception e)
        {
            lock (state)
            {
                state.ErrorOccurred = true;
                state.ErrorMessage = "Failed to begin connection. " + e.Message;

                state.OnNetworkAction(state);
            }
            return;
        }

        //Timeout code for 3 seconds of a failed connection. Found via google as recommended. Turns the state into a broken state.
        if (!result.AsyncWaitHandle.WaitOne(3000, true))
        {
            lock (state)
            {
                state.ErrorOccurred = true;
                state.ErrorMessage = "Connection timed out.";

                state.OnNetworkAction(state);
            }

            return;
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
    ///
    /// Uses EndConnect to finalize the connection.
    /// 
    /// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
    /// either this method or ConnectToServer should indicate the error appropriately.
    /// 
    /// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
    /// with a new SocketState representing the new connection.
    /// 
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginConnect</param>
    private static void ConnectedCallback(IAsyncResult ar)
    {
        SocketState state = (SocketState)ar.AsyncState!;
        Socket s = state.TheSocket;
        //Callbacking is simple; just invoke OnNetworkAction. If we can't, we make the state a broken state and return.
        try
        {
            s.EndConnect(ar);

            state.OnNetworkAction(state);
        }
        catch (Exception e)
        {
            state.ErrorOccurred = true;
            state.ErrorMessage = e.Message;

            state.OnNetworkAction(state);
            return;
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////
    // Server and Client Common Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
    /// as the callback to finalize the receive and store data once it has arrived.
    /// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
    /// 
    /// If anything goes wrong during the receive process, the SocketState's ErrorOccurred flag should 
    /// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
    /// OnNetworkAction should be invoked. Depending on when the error occurs, this should happen either
    /// in this method or in ReceiveCallback.
    /// </summary>
    /// <param name="state">The SocketState to begin receiving</param>
    public static void GetData(SocketState state)
    {
        Socket s = state.TheSocket;

        //We call BeginReceive with our current state, if something fails we say as such. The user handles the processing of data, we just receive it.
        try
        {
            s.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, ReceiveCallback, state);
        }
        catch (Exception e)
        {
            lock (state)
            {
                state.ErrorOccurred = true;
                state.ErrorMessage = "Failed to get data. " + e.Message;

                state.OnNetworkAction(state);
            }
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a receive operation that was initiated by GetData.
    /// 
    /// Uses EndReceive to finalize the receive.
    ///
    /// As stated in the GetData documentation, if an error occurs during the receive process,
    /// either this method or GetData should indicate the error appropriately.
    /// 
    /// If data is successfully received:
    ///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
    ///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
    ///      string builder.
    ///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
    /// </summary>
    /// <param name="ar"> 
    /// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
    /// </param>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        SocketState state = (SocketState) ar.AsyncState!;
        Socket s = state.TheSocket;
        int bytesRead;

        //We end the receive to actually get the bytes. If we can't, we failed, make a broken state. We then lock the state and parse the data, 
        //so that we don't end up parsing the data at the same time as something else parsing data (one buffer)
        try
        {
            bytesRead = s.EndReceive(ar);
        }
        catch (Exception e)
        {
            lock (state)
            {
                state.ErrorOccurred = true;
                state.ErrorMessage = "Failed to get data. " + e.Message;

                state.OnNetworkAction(state);
            }
            return;
        }

        lock (state.data)
        {
            ParseData(state, bytesRead);
        }

        // We then invoke the OnNetworkAction.
        state.OnNetworkAction(state);
       }

    /// <summary>
    /// Parses the data stored in the provided socket state's raw data buffer.
    /// Reads the data in as UTF-8 text and stores it in the state's stringbuilder data buffer.
    /// </summary>
    /// <param name="state"></param>
    private static void ParseData(SocketState state, int numBytes)
    {
        
        byte[] data = state.buffer;
        byte[] newData = new byte[numBytes];

        Array.Copy(data, newData, numBytes);

        string s = Encoding.UTF8.GetString(newData);

        state.data.Append(s);
    }

    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool Send(Socket socket, string data)
    {
        if (!socket.Connected)
        {
            return false;
        }
        //Encode our data, then try to send it. Otherwise, close the socket if we fail and return false.
        byte[] byteData = Encoding.UTF8.GetBytes(data);

        try
        {
            socket.BeginSend(byteData, 
                0, 
                byteData.Length, 
                SocketFlags.None, 
                SendCallback, 
                socket);

            return true;
        }
        catch
        {
            socket.Close();

            return false;
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by Send.
    ///
    /// Uses EndSend to finalize the send.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendCallback(IAsyncResult ar)
    {
        //Simple enough callback just closes the socket if we have an issue or ends the send.
        Socket s = (Socket) ar.AsyncState!;

        try
        {
            s.EndSend(ar);
        }
        catch
        {
            s.Close();
        }
    }


    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
    /// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool SendAndClose(Socket socket, string data)
    {
        if (!socket.Connected)
        {
            return false;
        }

        byte[] byteData = Encoding.UTF8.GetBytes(data);

        try
        {
            socket.BeginSend(byteData,
                0,
                byteData.Length,
                SocketFlags.None,
                SendAndCloseCallback,
                socket) ;

            return true;
        }
        catch
        {
            socket.Close();

            return false;
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
    ///
    /// Uses EndSend to finalize the send, then closes the socket.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// 
    /// This method ensures that the socket is closed before returning.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendAndCloseCallback(IAsyncResult ar)
    {
        Socket s = (Socket) ar.AsyncState!;

        try
        {
            s.EndSend(ar);
        }
        catch
        {
        }
        //We always wanna close, so this doesn't need to be in the catch.
        s.Close();
    }
}
