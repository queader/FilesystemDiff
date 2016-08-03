using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Directory = Delimon.Win32.IO.Directory;
using FileInfo = Delimon.Win32.IO.FileInfo;
using Path = Delimon.Win32.IO.Path;

namespace FilesystemDiff
{
    class Program
    {
        static System.IO.BinaryWriter binaryWriter;

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            string diskLetter = @"C:\";

            //using (var fileStream = new System.IO.FileStream(@"R:\filesystem_scan.bin", System.IO.FileMode.Create))
            //using (binaryWriter = new System.IO.BinaryWriter(fileStream))
            //{
            //    //foreach (var logicalDrive in Directory.GetLogicalDrives())
            //    //{
            //    //    ScanDrive(logicalDrive);
            //    //}

            //    ScanDrive(diskLetter);
            //}

            string fileNameTimestamp = string.Format($@"FilesystemScans\{GetNameTimestamp()}.fscan");

            System.IO.Directory.CreateDirectory("FilesystemScans");

            using (var memoryStream = new MemoryStream(1024 * 1024 * 64))
            using (binaryWriter = new System.IO.BinaryWriter(memoryStream))
            {
                ScanDrive(diskLetter);

                using (var fileStream = new System.IO.FileStream(fileNameTimestamp, System.IO.FileMode.Create))
                using (var zipStream = new GZipStream(fileStream, CompressionLevel.Optimal, false))
                {
                    zipStream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                }
            }

            Console.WriteLine("Finished.");
            //Console.ReadLine();
        }

        static string GetNameTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }

        static void ScanDrive(string drive)
        {
            string currentDrive = Path.GetPathRoot(drive);

            Console.WriteLine($"Scanning Drive: {currentDrive}");
            //Console.WriteLine($"{currentDrive}");

            WriteDriveTag(currentDrive);

            try
            {
                foreach (var directory in Directory.GetDirectories(drive))
                {
                    ScanDirectoryRecursive(directory);
                }
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine($"Cannot Scan Drive: {drive}");
            }

            EndDriveTag();
        }

        static int recursionLevel = 0;

        static void ScanDirectoryRecursive(string path)
        {
            ++recursionLevel;

            //string currentDirectory = Path.GetFileName(Path.GetDirectoryName(path));
            string currentDirectory = Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(currentDirectory))
            {
                Console.WriteLine($"Ignoring Directory: {currentDirectory}");
                return;
            }

            //for (int n = 0; n < recursionLevel; ++n)
            //{
            //    Console.Write("  ");
            //}
            //Console.WriteLine($"Scanning Directory: {currentDirectory}");
            //Console.WriteLine($"{currentDirectory}");

            WriteDirectoryTag(currentDirectory);

            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    ScanFile(file);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            try
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    ScanDirectoryRecursive(directory);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            EndDirectoryTag();

            --recursionLevel;
        }

        static long ScanFile(string path)
        {
            string fileName = Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                Console.WriteLine($"Ignoring File: {fileName}");
            }

            long size = GetFileSizeOnDisk(path);

            //for (int n = 0; n < recursionLevel + 1; ++n)
            //{
            //    Console.Write("  ");
            //}
            //Console.WriteLine($"Scanning File: {path}, Size: {size}");

            WriteFileTag(fileName, size);

            return size;
        }

        static void WriteFileTag(string name, long size)
        {
            binaryWriter.Write((byte)5);
            binaryWriter.Write(name);
            binaryWriter.Write(size);
        }

        static void WriteDriveTag(string name)
        {
            binaryWriter.Write((byte)3);
            binaryWriter.Write(name);
        }

        static void EndDriveTag()
        {
            binaryWriter.Write((byte)4);
        }

        static void WriteDirectoryTag(string name)
        {
            binaryWriter.Write((byte)1);
            binaryWriter.Write(name);
        }

        static void EndDirectoryTag()
        {
            binaryWriter.Write((byte)2);
        }

        public static long GetFileSizeOnDisk(string file)
        {
            return new FileInfo(file).Length;
        }

        public static long GetFileSizeOnDiskAdv(string file)
        {
            FileInfo info = new FileInfo(file);
            uint dummy, sectorsPerCluster, bytesPerSector;
            //int result = GetDiskFreeSpaceW(info.Directory.Root.FullName, out sectorsPerCluster, out bytesPerSector, out dummy, out dummy);
            int result = GetDiskFreeSpaceW(info.Directory.Root, out sectorsPerCluster, out bytesPerSector, out dummy, out dummy);
            if (result == 0) throw new Win32Exception();
            uint clusterSize = sectorsPerCluster * bytesPerSector;
            uint hosize;
            uint losize = GetCompressedFileSizeW(file, out hosize);
            long size;
            size = (long)hosize << 32 | losize;
            return ((size + clusterSize - 1) / clusterSize) * clusterSize;
        }

        [DllImport("kernel32.dll")]
        static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
           [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
           out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
           out uint lpTotalNumberOfClusters);
    }
}
