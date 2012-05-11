using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libopenpvp.net
{
    class TypedObject : Dictionary<String, Object>
    {
        public String Type;

        /// <summary>
        /// Initializes the type of the object
        /// null type  implies a dynamic object (userd for headers)
        /// </summary>
        /// <param name="type"> The type of the object</param>
        public TypedObject(String type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Creates a flex.messaging.io.ArrayCollection in the structure that the encoder expects
        /// </summary>
        /// <param name="data">The data for the ArrayCollection</param>
        /// <returns>The TypedObject representing the ArrayCollection</returns>
        public static TypedObject makeArrayCollection(Object[] data)
        {
            TypedObject ret = new TypedObject("flex.messaging.io.ArrayCollection");
            ret.Add("array", data);
            return ret;
        }

    }
}
