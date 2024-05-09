using System;
using System.IO;

namespace VivaldiUpdater.Helpers
{
    public static class BinaryDetection
    {
        public static bool IsX64Image(string filepath)
        {
            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                if (br.ReadUInt16() != 0x5A4D) // 检查MZ头
                    throw new BadImageFormatException("Not a valid Portable Executable image", filepath);
                fs.Position = 0x3C;
                var peHeaderPointer = br.ReadUInt32();
                fs.Position = peHeaderPointer;
                var peHeader = br.ReadUInt32();
                if (peHeader != 0x00004550) // 检查PE\0\0标志
                    throw new BadImageFormatException("Not a valid Portable Executable image", filepath);
                var machine = br.ReadUInt16();
                // 机器类型: 0x8664表示x64, 0x014C表示x86
                return machine == 0x8664;
            }
        }
    }
}