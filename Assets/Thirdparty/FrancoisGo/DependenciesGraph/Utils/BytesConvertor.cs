using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DependenciesGraph
{
    public static class BytesConvertor
    {
        public static byte[] ToByteArray<T>(T obj)
        {
            if (obj == null)
                return null;

            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        public static T FromByteArray<T>(byte[] data)
        {
            if (data == null)
                return default(T);

            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream(data))
            {
                object obj = binaryFormatter.Deserialize(memoryStream);
                return (T)obj;
            }
        }
    }
}
