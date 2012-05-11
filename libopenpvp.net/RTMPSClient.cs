using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

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
        protected NetworkStream output;
        protected BufferedStream input;
        protected RTMPPacketReader pr;

        ///** State information */
        protected bool connected = false;
        //protected int invokeID = 2;

        ///** Used for generating handshake */
        protected Random rand = new Random();

        /** Encoder */
        protected AMF3Encoder aec = new AMF3Encoder();
        
        ///** For error tracking */
        //public TypedObject lastDecode = null;
        
        ///** Pending invokes */
        //protected Set<Integer> pendingInvokes = Collections.synchronizedSet(new HashSet<Integer>());
        
        ///** Callback list */
        //protected Map<Integer, Callback> callbacks = new HashMap<Integer, Callback>();

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


            output = new NetworkStream(sslsocket);
            input = new BufferedStream(output);
            
            doHandshake();

            // Start reading responses
            pr = new RTMPPacketReader(output);

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
                input.Write(connect, 0, connect.Length);
                input.Flush();

                TypedObject result = pr.GetPacket(1);
                TypedObject body = (TypedObject)result.Get("body");
                DSId = (string)body.Get("id");

                connected = true;
            }
            catch (Exception e)
            {

                throw new Exception("Error encoding or decoding", e);
            }




        }

        private void doHandshake()
        {
            // C0
            byte C0 = 0x03;
            input.WriteByte(C0);

            // C1
            long timestampC1 = Utilities.CurrentTimeMillis();
            byte[] randC1 = new byte[1528];
            rand.NextBytes(randC1);
  



        }





    }
}
