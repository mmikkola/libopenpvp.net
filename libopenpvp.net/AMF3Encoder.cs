using System;
using System.Collections.Generic;
using System.IO;

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
        
        /// <summary>
        /// Encodes the given data as a connect packet
        /// </summary>
        /// <param name="id">The invoke ID</param>
        /// <param name="data">The data to invoke</param>
        /// <returns></returns>
        public byte[] encodeInvoke(int id, Object data)
        {
            List<byte> result = new List<byte>();

            result.Add(0x00); // Version
            result.Add(0x05); // Type?
            writeIntAMFO(result, id); // Invoke ID
            result.Add(0x05); // ???

            result.Add(0x11); // AMF3 Object

            encode(result, data);

            return result.ToArray();
        }

        /// <summary>
        /// Encodes an object as AMF3
        /// </summary>
        /// <param name="obj">The object to encode</param>
        /// <returns>The encoded object</returns>
        public byte[] encode(Object obj)
        {
            List<byte> result = new List<byte>();

            encode(result, obj);
            return result.ToArray();
        }

        /// <summary>
        /// Encodes an object as AMF3 to the give buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="obj">The object to encode</param>
        private void encode(List<byte> ret, Object obj)
        {
            if (obj == null)
            {
                ret.Add(0x01);
            }
            else if (obj is Boolean)
            {
                bool val = (bool) obj;

                if(val)
                {
                    ret.Add(0x03);
                }
                else
                {
                    ret.Add(0x02);
                }
            }
            else if (obj is int)
            {
                ret.Add(0x04);
                writeInt(ret, (int) obj);
            }
            else if (obj is double)
            {
                ret.Add(0x05);
                writeDouble(ret, (double) obj);
            }
            else if (obj is String)
            {
                ret.Add(0x06);
                writeString(ret, (String) obj);
            }
            else if (obj is DateTime)
            {
                ret.Add(0x08);
                writeDateTime(ret, (DateTime) obj);
            }
            else if (obj is byte[])
            {
                ret.Add(0x0C);
                writeByteArray(ret, (byte[]) obj);
            }
            else if (obj is Object[])
            {
                ret.Add(0x09);
                writeArray(ret, (Object[]) obj);
            }
            else if (obj is TypedObject)
            {
                ret.Add(0x0A);
                writeObject(ret, (TypedObject) obj);
            }
            else if (obj is Dictionary<String,Object>)
            {
                ret.Add(0x0A);
                writeAssociativeArray(ret, (Dictionary<String, Object>) obj);            
            }
            else
            {
                throw new EncodingException("Unexpected Object Type: " + obj);
            }
        }

        private void writeObject(List<byte> ret, TypedObject typedObject)
        {
            throw new NotImplementedException();
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

        private void writeArray(List<byte> ret, object[] objects)
        {
            throw new NotImplementedException();
        }

        private void writeByteArray(List<byte> ret, byte[] bytes)
        {
            throw new NotImplementedException();
        }

        private void writeDateTime(List<byte> ret, DateTime dateTime)
        {
            throw new NotImplementedException();
        }

        private void writeString(List<byte> ret, string s)
        {
            throw new NotImplementedException();
        }

        private void writeDouble(List<byte> ret, double val)
        {
            if(Double.IsNaN(val))
            {
                ret.Add((byte)0x7F);
                ret.Add((byte)0xFF);
                ret.Add((byte)0xFF);
                ret.Add((byte)0xFF);
                ret.Add((byte)0xE0);
                ret.Add((byte)0x00);
                ret.Add((byte)0x00);
                ret.Add((byte)0x00);
            }
            else
            {
                byte[] temp = new byte[8];
                MemoryStream stream = new MemoryStream(temp);
                BinaryWriter binaryWriter = new BinaryWriter(stream);
                binaryWriter.Write(val);
                foreach(byte b in temp)
                {
                    ret.Add(b);
                }
                
            }
        }

        /// <summary>
        /// Encodes and integer as AMF3 to a given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="val">The integer to encode</param>
        private void writeInt(List<byte> ret, int val)
        {

            if (val < 0 || val >= 0x200000)
            {
                ret.Add((byte)(((val >> 22) & 0x7f) | 0x80));
                ret.Add((byte)(((val >> 15) & 0x7f) | 0x80));
                ret.Add((byte)(((val >> 8) & 0x7f) | 0x80));
                ret.Add((byte)(val & 0xff));
            }
            else
            {
                if (val >= 0x4000)
                {
                    ret.Add((byte)(((val >> 14) & 0x7f) | 0x80));
                }
                if (val >= 0x80)
                {
                    ret.Add((byte)(((val >> 7) & 0x7f) | 0x80));
                }
                ret.Add((byte)(val & 0x7f));
            }
        }

        private void writeStringAMFO(List<byte> result, string connect)
        {
            throw new NotImplementedException();
        }
    }
}