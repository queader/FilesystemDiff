using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpFilesystemScan
{
    class Program
    {
        static BinaryReader binaryReader;

        static int recursionLevel = 0;

        static void Main(string[] args)
        {
            binaryReader = new BinaryReader(new FileStream(@"R:\filesystem_scan.bin", FileMode.Open));

            do
            {
                ReadNextTag();
            } while (recursionLevel > 0);
        }

        static string currentDirectory = "";

        static void ReadNextTag()
        {
            byte tag = binaryReader.ReadByte();

            switch (tag)
            {
                case 3:
                {
                    ProcessStartDriveTag();
                    break;
                }
                case 4:
                {
                    ProcessEndDriveTag();
                    break;
                }
                case 1:
                {
                    ProcessStartDirectoryTag();
                    break;
                }
                case 2:
                {
                    ProcessEndDirectoryTag();
                    break;
                }
                case 5:
                {
                    ProcessFileTag();
                    break;
                }
            }
        }

        static void ProcessFileTag()
        {
            string fileName = CleanFileName(binaryReader.ReadString());
            long fileSize = binaryReader.ReadInt64();

            PrintCurrentFile(fileName, fileSize);
        }

        static void PrintCurrentFile(string name, long size)
        {
            for (int n = 0; n < recursionLevel; ++n)
            {
                Console.Write("  ");
            }

            Console.WriteLine($"{Path.Combine(currentDirectory, name)} : {size}");
        }

        static void ProcessEndDirectoryTag()
        {
            --recursionLevel;

            currentDirectory = Path.GetDirectoryName(currentDirectory);
            //Console.WriteLine($"End Dir: {currentDirectory}");
        }

        static void ProcessStartDirectoryTag()
        {
            ++recursionLevel;

            string directoryName = CleanFileName(binaryReader.ReadString());
            currentDirectory = Path.Combine(currentDirectory, directoryName);

            PrintCurrentDirectory();
        }

        static void ProcessEndDriveTag()
        {
            --recursionLevel;

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        static void ProcessStartDriveTag()
        {
            ++recursionLevel;

            string driveName = binaryReader.ReadString();
            currentDirectory = Path.Combine(currentDirectory, driveName);

            PrintCurrentDirectory();
        }

        static void PrintCurrentDirectory()
        {
            for (int n = 0; n < recursionLevel - 1; ++n)
            {
                Console.Write("  ");
            }

            Console.WriteLine(currentDirectory);
        }

        static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

    }
}
