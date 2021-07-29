using System;
using System.Reflection;
using System.IO;
using System.Text;
using K4os.Compression.LZ4;

namespace intouch
{
    class Program
    {
        static void Main(string[] args)
        {
            var dllPath = Path.GetFullPath(@"./apk/unknown/assemblies/InTouchLib_Generic.dll");
            if (args.Length > 0)
            {
                dllPath = Path.GetFullPath(args[0]);
            }

            // xamarin decompress dll (if required)
            byte[] bytes;
            byte[] head = new byte[12];
            var file = File.OpenRead(dllPath);
            file.Read(head, 0, 12);
            if (System.Text.Encoding.UTF8.GetString(head[0..4]) == "XALZ")
            {
                // determine size
                uint size = BitConverter.ToUInt32(head[8..12]);
                // reinit
                bytes = new byte[size];
                Console.WriteLine("Decompressing...");
                using (MemoryStream reader = new MemoryStream())
                {
                    file.CopyTo(reader);
                    LZ4Codec.Decode(reader.ToArray(), bytes);
                }
            }
            else
            {
                // dll seems to already be decompressed
                using (MemoryStream reader = new MemoryStream())
                {
                    reader.Write(head);
                    file.CopyTo(reader);
                    bytes = reader.ToArray();
                }
            }

            Assembly assembly = Assembly.Load(bytes);

            // create data reader
            var intouchdata = assembly.CreateInstance("InTouchLib.Util.InTouchData");

            // load xml stream
            var resstm = assembly.GetManifestResourceStream("InTouchLib.Resources.SpaPackStruct.xml");
            var br = new BinaryReader(resstm);
            var d1 = br.ReadBytes((int)resstm.Length);

            // read into bytes
            byte[] d2 = (byte[])intouchdata.GetType().InvokeMember("Read", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, intouchdata, new Object[] { d1 });

            // encode and export
            var xml = Encoding.GetEncoding("ISO-8859-1").GetString(d2);
            File.WriteAllText("SpaPackStruct.xml", xml.TrimEnd('\0'));

            Console.WriteLine("Saved as SpaPackStruct.xml");
        }
    }
}
