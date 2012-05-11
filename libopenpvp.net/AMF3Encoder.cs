using System;
using System.Collections.Generic;

namespace libopenpvp.net
{
    internal class AMF3Encoder
    {
        /** RNG used for generating MessageIDs */
        private static Random _rand = new Random();

        /** Used for generating timestamps in headers */
        private readonly long _startTime = CurrentTimeMillis();


        private static long CurrentTimeMillis()
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long) (DateTime.UtcNow - unixEpoch).TotalMilliseconds;
        }

        /// <summary>
        /// Adds headers to provided data.
        /// </summary>
        /// <param name="data">The data to add headers to.</param>
        /// <returns>The data with added headers.</returns>
        public byte[] AddHeaders(byte[] data)
        {
            var result = new List<byte>();

            // Header byte
            result.Add(0x03);

            // Timestamp
            long timediff = CurrentTimeMillis() - _startTime;
            result.Add((byte) ((timediff & 0xFF0000) >> 16));
            result.Add((byte) ((timediff & 0x00FF00) >> 8));
            result.Add((byte) (timediff & 0x0000FF));

            // Content type
            result.Add(0x11);

            // Source ID
            result.Add(0x00);
            result.Add(0x00);
            result.Add(0x00);
            result.Add(0x00);

            // Add body
            for (int i = 0; i < data.Length; ++i)
            {
                result.Add(data[i]);
                if (i%128 == 127 && i < data.Length - 1)
                {
                    result.Add(0xC3);
                }
            }


            return result.ToArray();
        }

        /// <summary>
        /// Encodes the given parameters as a connect packet
        /// </summary>
        /// <param name="parameters">The connection parameters.</param>
        /// <returns>The connect packet.</returns>
        public byte[] encodeConnect(Dictionary<String, Object> parameters)
        {
            var result = new List<byte>();

            writeStringAMFO(result, "connect");
            // Write invokeid
            writeIntAMFO(result, 1);

            // Write params
            result.Add(0x11); // AMF3 object
            result.Add(0x09); // Array
            writeAssociativeArray(result, parameters);

            // Write service call args
            result.Add(0x01);
            result.Add(0x00); // False
            writeStringAMFO(result, "nil");
            writeStringAMFO(result, "");

            // Set up CommandMessage
            var cm = new TypedObject("flex.messaging.messages.CommandMessage");
            cm.Add("messageRefType", null);
            cm.Add("operation", 5);
            cm.Add("correlationId", "");
            cm.Add("clientId", null);
            cm.Add("destination", "");
            cm.Add("messageId", randomUID());
            cm.Add("timestamp", 0d);
            cm.Add("timeToLive", 0d);
            cm.Add("body", new TypedObject(null));
            var headers = new Dictionary<string, object>();

            headers.Add("DSMessagingVersion", 1d);
            headers.Add("DSId", "my-rtmps");
            cm.Add("headers", headers);

            // Write CommandMessage
            result.Add(0x11); // AMF3 Object
            encode(result, cm);

            return AddHeaders(result.ToArray());
        }

        private void writeIntAMFO(List<byte> result, int i)
        {
            throw new NotImplementedException();
        }

        private void writeAssociativeArray(List<byte> result, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        private object randomUID()
        {
            throw new NotImplementedException();
        }

        private void encode(List<byte> result, TypedObject cm)
        {
            throw new NotImplementedException();
        }

        private void writeStringAMFO(List<byte> result, string connect)
        {
            throw new NotImplementedException();
        }
    }
}