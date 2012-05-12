using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace libopenpvp.net
{
    public class RTMPSClient
    {
        protected string server;
        protected int port;
        protected string app;
        protected string swfUrl;
        protected string pageUrl;

        /** Connection information */
        protected string DSId;
        
        /** Socket and streams */
        protected Socket sslsocket;
        protected NetworkStream input;
        protected BufferedStream output;
        protected RTMPPacketReader pr;

        ///** State information */
        protected bool connected = false;
        protected int invokeID = 2;

        ///** Used for generating handshake */
        protected Random rand = new Random();

        /** Encoder */
        protected AMF3Encoder aec = new AMF3Encoder();
        
        ///** For error tracking */
        public TypedObject lastDecode = null;
        
        ///** Pending invokes */
        protected HashSet<int> pendingInvokes = new HashSet<int>();
        
        ///** Callback list */
        protected Dictionary<int, AsyncCallback> callbacks = new Dictionary<int, AsyncCallback>();

        protected RTMPSClient() { }

        /// <summary>
        /// Sets up the client with given parameters
        /// </summary>
        /// <param name="server">The RTMPS server address</param>
        /// <param name="port">the RTMPS server port</param>
        /// <param name="app">The app to use in connect call</param>
        /// <param name="swfUrl">the swf URL to use in connect call</param>
        /// <param name="pageUrl">the page URL to use in connect call</param>
        public RTMPSClient(string server, int port, string app, string swfUrl, string pageUrl)
        {
            this.server = server;
            this.port = port;

            this.app = app;
            this.swfUrl = swfUrl;
            this.pageUrl = pageUrl;
        }

        /// <summary>
        /// Sets up the client with given parameters
        /// </summary>
        /// <param name="server">The RTMPS server address</param>
        /// <param name="port">the RTMPS server port</param>
        /// <param name="app">The app to use in connect call</param>
        /// <param name="swfUrl">the swf URL to use in connect call</param>
        /// <param name="pageUrl">the page URL to use in connect call</param>
        public void SetConnectionInfo(string server, int port, string app, string swfUrl, string pageUrl)
        {
            this.server = server;
            this.port = port;

            this.app = app;
            this.swfUrl = swfUrl;
            this.pageUrl = pageUrl;
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            pr.Die();
            sslsocket.Shutdown(SocketShutdown.Both);
            sslsocket.Close();
        }

        public void Connect()
        {

            //not sure if this socket will work but it should work...
            sslsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //create socket pointing at variable server and port 2099
            IPHostEntry hostEntry = Dns.GetHostEntry(server);
            IPEndPoint ipe = new IPEndPoint(hostEntry.AddressList.First(), 2099);
            sslsocket.Connect(ipe);


            input = new NetworkStream(sslsocket);
            output = new BufferedStream(input);
            
            doHandshake();

            // Start reading responses
            pr = new RTMPPacketReader(input);

            // Handle preconnect Messages?
            // -- 02 | 00 00 00 | 00 00 05 | 06 00 00 00 00 | 00 03 D0 90 02

            // Connect
            Dictionary<String, Object> parameters = new Dictionary<String, Object>();
            parameters.Add("app", app);
            parameters.Add("flashVer", "WIN 10,1,85,3");
            parameters.Add("swfUrl", swfUrl);
            parameters.Add("tcUrl", "rtmps://" + server + ":" + port);
            parameters.Add("fpad", false);
            parameters.Add("capabilities", 239);
            parameters.Add("audioCodecs", 3191);
            parameters.Add("videoCodecs", 252);
            parameters.Add("videoFunction", 1);
            parameters.Add("pageUrl", pageUrl);
            parameters.Add("objectEncoding", 3);

            try
            {
                byte[] connect = aec.encodeConnect(parameters);

                // note NetworkStream doesn't have a reliable Flush() method. anything in Write Method will immediately be sent to server.
                output.Write(connect, 0, connect.Length);
                output.Flush();

                TypedObject result = pr.GetPacket(1);
                TypedObject body = (TypedObject)result["body"];
                DSId = (string)body["id"];

                connected = true;
            }
            catch (Exception e)
            {
                throw new Exception("Error encoding or decoding", e);
            }
        }

        /// <summary>
        /// Executes a full RTMP handshake
        /// </summary>
        private void doHandshake()
        {
            BinaryWriter binWriter = new BinaryWriter(output);

            // C0
            byte C0 = 0x03;
            binWriter.Write(C0);

            // C1
            long timestampC1 = Utilities.CurrentTimeMillis();
            byte[] randC1 = new byte[1528];
            rand.NextBytes(randC1);

            binWriter.Write((int)timestampC1);
            binWriter.Write(0);
            binWriter.Write(randC1, 0, 1528);
            binWriter.Flush();

            // S0
            byte S0 = (byte)input.ReadByte();
            if (S0 != 0x03)
                throw new Exception("Server returned incorrect version in handshake: " + S0);

            // S1
            byte[] S1 = new byte[1536];
            input.Read(S1, 0, 1536);

            // C2
            long timestampC2 = Utilities.CurrentTimeMillis();
            binWriter.Write(S1, 0, 4);
            binWriter.Write((int)timestampC1);
            binWriter.Write(S1, 8, 1528);
            binWriter.Flush();

            // S2
            byte[] S2 = new byte[1536];
            input.Read(S1, 0, 1536);

            // Validate handshake
            bool valid = true;
            for (int i = 8; i < 1536; i++)
                if (randC1[i - 8] != S2[i])
                {
                    valid = false;
                    break;
                }

            if (!valid)
                throw new Exception("Handshake was not valid.");
        }

        /// <summary>
        /// Invokes something.
        /// </summary>
        /// <param name="destination">The destination</param>
        /// <param name="operation">The operation</param>
        /// <param name="body">The arguments</param>
        /// <returns>The invoke ID to use with getResult(), peekResult(), and join()</returns>
        public int WriteInvoke(string destination, object operation, object body)
        {
            int id = nextInvokeID();
            pendingInvokes.Add(id);

            TypedObject wrapped = wrapBody(body, destination, operation);
            byte[] data = aec.encodeInvoke(id, wrapped);
            output.Write(data, 0, data.Length);
            output.Flush();

            return id;
        }

        /// <summary>
        /// Invokes something asynchronously.
        /// </summary>
        /// <param name="destination">The destination</param>
        /// <param name="operation">The operation</param>
        /// <param name="body">The arguments</param>
        /// <param name="cb">The AsyncCallback that will recieve the result.</param>
        /// <returns>The invoke ID to use with getResult(), peekResult(), and join()</returns>
        public int WriteInvokeWithCallback(string destination, object operation, object body, AsyncCallback cb)
        {
            int id = nextInvokeID();
            callbacks.Add(id, cb);
            pendingInvokes.Add(id);

            TypedObject wrapped = wrapBody(body, destination, operation);
            byte[] data = aec.encodeInvoke(id, wrapped);
            output.Write(data, 0, data.Length);

            return id;

        }

        /// <summary>
        /// Sets up a body in a full RemotingMessage With headers, etc.
        /// </summary>
        /// <param name="body">The body to wrap.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="operation">The operation</param>
        /// <returns></returns>
        protected TypedObject wrapBody(object body, string destination, object operation)
        {
            TypedObject headers = new TypedObject(null);
            headers.Add("DSRequestTimeout", 60);
            headers.Add("DSId", DSId);
            headers.Add("DSEndpoint", "my-rtmps");

            TypedObject ret = new TypedObject("flex.messaging.messages.RemotingMessage");
            ret.Add("destination", destination);
            ret.Add("operation", operation);
            ret.Add("source", null);
            ret.Add("timestamp", 0);
            ret.Add("messageId", AMF3Encoder.randomUID());
            ret.Add("timeToLive", 0);
            ret.Add("clientId", null);
            ret.Add("headers", headers);
            ret.Add("body", body);

            return ret;
        }

        /// <summary>
        /// Returns the next invoke ID to use.
        /// </summary>
        /// <returns>The next invoke ID.</returns>
        protected int nextInvokeID()
        {
            return invokeID++;
        }

        /// <summary>
        /// Returns the next invoke ID
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return (connected && pr.running);
        }

        /// <summary>
        /// Removes and returns a result for a given invoke ID if it's ready.
        /// </summary>
        /// <param name="id">The invoke ID.</param>
        /// <returns>The invoke's result or null.</returns>
        public TypedObject PeekResult(int id)
        {
            return pr.PeekPacket(id);
        }

        /// <summary>
        /// Blocks and waits for the invoke's result to be ready. then removes and returns it.
        /// </summary>
        /// <param name="id">The invoke ID.</param>
        /// <returns>The invoke's result.</returns>
        public TypedObject GetResult(int id)
        {
            return pr.GetPacket(id);
        }

        /// <summary>
        /// Waits until all results have been returned.
        /// </summary>
        public void join()
        {
            while (pendingInvokes.Count > 0)
            {
                try
                {
                    Thread.Sleep(10);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Waits until specified result returns.
        /// </summary>
        /// <param name="id"></param>
        public void join(int id)
        {
            while (pendingInvokes.Contains(id))
            {
                try
                {
                    Thread.Sleep(10);
                }
                catch (Exception) { }
            }
        }

        class RTMPPacketReader : Thread
        {

        }



    }
}
