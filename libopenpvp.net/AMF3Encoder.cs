using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace libopenpvp.net
{
    public class AMF3Encoder
    {
        /** RNG used for generating MessageIDs */
        private static Random _rand = new Random();

        /** Used for generating timestamps in headers */
        private readonly long _startTime = Utilities.CurrentTimeMillis();

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
            long timediff = Utilities.CurrentTimeMillis() - _startTime;
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
        /// Encodes the given parameters as a val packet
        /// </summary>
        /// <param name="parameters">The connection parameters.</param>
        /// <returns>The val packet.</returns>
        public byte[] encodeConnect(Dictionary<string, object> parameters)
        {
            var result = new List<byte>();

            writestringAMFO(result, "val");
            // Write invokeid
            writeIntAMFO(result, 1);

            // Write params
            result.Add(0x11); // AMF3 object
            result.Add(0x09); // Array
            writeAssociativeArray(result, parameters);

            // Write service call args
            result.Add(0x01);
            result.Add(0x00); // False
            writestringAMFO(result, "nil");
            writestringAMFO(result, "");

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
            result.Add(0x11); // AMF3 object
            encode(result, cm);

            return AddHeaders(result.ToArray());
        }
        
        /// <summary>
        /// Encodes the given data as a val packet
        /// </summary>
        /// <param name="id">The invoke ID</param>
        /// <param name="data">The data to invoke</param>
        /// <returns></returns>
        public byte[] encodeInvoke(int id, object data)
        {
            List<byte> result = new List<byte>();

            result.Add(0x00); // Version
            result.Add(0x05); // Type?
            writeIntAMFO(result, id); // Invoke ID
            result.Add(0x05); // ???

            result.Add(0x11); // AMF3 object

            encode(result, data);

            return result.ToArray();
        }

        /// <summary>
        /// Encodes an object as AMF3
        /// </summary>
        /// <param name="obj">The object to encode</param>
        /// <returns>The encoded object</returns>
        public byte[] encode(object obj)
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
        private void encode(List<byte> ret, object obj)
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
            else if (obj is string)
            {
                ret.Add(0x06);
                writeString(ret, (string) obj);
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
            else if (obj is object[])
            {
                ret.Add(0x09);
                writeArray(ret, (object[]) obj);
            }
            else if (obj is TypedObject)
            {
                ret.Add(0x0A);
                writeobject(ret, (TypedObject) obj);
            }
            else if (obj is Dictionary<string,object>)
            {
                ret.Add(0x0A);
                writeAssociativeArray(ret, (Dictionary<string, object>) obj);            
            }
            else
            {
                throw new EncodingException("Unexpected object Type: " + obj);
            }
        }

        /// <summary>
        /// Encodes an object as AMF3 to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="val">The object to encode</param>
        private void writeobject(List<byte> ret, TypedObject val)
        {
            if (val.Type == null || val.Type.Equals(""))
            {
                ret.Add(0x08); // Dynamic class with no members
                ret.Add(0x01); // No class name
                foreach(string key in val.Keys)
                {
                    writeString(ret, key);
                    encode(ret, val[key]);
                }
                ret.Add(0x01); // End of dynamic
            }
            else if (val.Type.Equals("flex.messaging.io.ArrayCollection"))
            {
                ret.Add(0x07); // Externalizeable
                writeString(ret, val.Type);
                encode(ret, val["array"]);
            }
            else
            {
                writeInt(ret, (val.Count << 4) | 3); // Inline + member count
                writeString(ret, val.Type);

                List<string> keyOrder = new List<string>();
                foreach (string key in val.Keys)
                {
                    writeString(ret, key);
                    keyOrder.Add(key);
                }
                foreach (string s in keyOrder)
                {
                    encode(ret, val[s]);
                }

            }
        }

        /// <summary>
        /// Encodes an integer as AMFO to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="val">The integer to encode</param>
        private void writeIntAMFO(List<byte> ret, int val)
        {
            ret.Add(0x00);

            byte[] temp = new byte[8];
            MemoryStream memoryStream = new MemoryStream(temp);
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(val);

            foreach (byte b in temp)
            {
                ret.Add(b);
            }
        }

        /// <summary>
        /// Encodes an Associative Array as AMF3 to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="val">The Array to encode</param>
        private void writeAssociativeArray(List<byte> ret, Dictionary<string, object> val)
        {
            ret.Add(0x01);

            foreach(string key in val.Keys)
            {
                writeString(ret, key);
                encode(ret, val[key]);
            }
            ret.Add(0x01);
        }

        /// <summary>
        /// Encodes an array of objects as AMF3 to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="objects">The array to encode</param>
        private void writeArray(List<byte> ret, object[] objects)
        {
            writeInt(ret, (objects.Length << 1) | 1);
            ret.Add(0x01);
            foreach (object o in objects)
            {
                encode(ret, o);
            }
        }

        private void writeByteArray(List<byte> ret, byte[] bytes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Encodes a DateTime object as AMF3 to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="dateTime">The DateTime object to encode</param>
        private void writeDateTime(List<byte> ret, DateTime dateTime)
        {
            ret.Add(0x01);

            writeDouble(ret, (double) Utilities.GetTimeMillis(dateTime));
        }

        /// <summary>
        /// Writes a string as AMF3 to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="val">The string to encode</param>
        private void writeString(List<byte> ret, string val)
        {
            writeInt(ret, (val.Length << 1) | 1);

            UTF8Encoding utf8 = new UTF8Encoding();

            ret.AddRange(utf8.GetBytes(val));
        }

        /// <summary>
        /// Encodes a double as AMF3 to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="val">The double to encode</param>
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

        /// <summary>
        /// Encodes a string as AMFO to the given buffer
        /// </summary>
        /// <param name="ret">The buffer to encode to</param>
        /// <param name="val">The string to encode</param>
        private void writestringAMFO(List<byte> ret, string val)
        {
            byte[] temp = null;
            UTF8Encoding utf8 = new UTF8Encoding();
            temp = utf8.GetBytes(val);

            ret.Add(0x02);

            ret.Add((byte) ((temp.Length & 0xFF00) >> 8));
            ret.Add((byte) (temp.Length & 0x00FF));

            foreach (byte b in temp)
            {
                ret.Add(b);
            }

        }

        public static string randomUID()
        {
            byte[] bytes = new byte[16];

            _rand.NextBytes(bytes);

            StringBuilder ret = new StringBuilder();
            for(int i = 0; i < bytes.Length; i++)
            {
                if (i == 4 || i == 6 || i == 8 || i == 10)
                {
                    ret.Append('-');
                }
                ret.Append(string.Format("{0,2,X}", bytes[i]));
            }

            return ret.ToString();
        }
    }
}