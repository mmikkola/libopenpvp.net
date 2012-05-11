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
        protected Stream input; //inputStream
        protected Stream output; // DataOutPutStream
        protected RTMPPacketReader pr;

        ///** State information */
        //protected boolean connected = false;
        //protected int invokeID = 2;

        ///** Used for generating handshake */
        //protected Random rand = new Random();

        ///** Encoder */
        //protected AMF3Encoder aec = new AMF3Encoder();
        
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
            sslsocket = new Socket() //create socket pointing at variable server and port 2099
            
            doHandshake();

            // Start reading responses
            pr = new RTMPPacketReader(input);



        }

        private void doHandshake()
        {
            throw new NotImplementedException();
        }





    }
}
