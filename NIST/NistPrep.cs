using System;
using System.IO;
using System.Security.Cryptography;

class NistPrep
{
    const int STREAM_COUNT = 100;
    const int BITS_PER_STREAM = 500000;
    const int BYTES_PER_STREAM = BITS_PER_STREAM / 8; // 62500
    const int HASH_SIZE = 32; // SHA256 = 32 bytes
    const int HASHES_PER_STREAM = BYTES_PER_STREAM / HASH_SIZE; // 195
    const int CHUNK_SIZE = HASH_SIZE; // 32 bytes per chunk

    static void Main()
    {
        string inputPath = "accumulated_salt.txt";  // Input = raw binary
        string outputPath = "flattened_salt.bin";   // Output = for NIST

        if (!File.Exists(inputPath))
        {
            Console.WriteLine("No accumulated salt file found.");
            Pause();
            return;
        }

        byte[] allData = File.ReadAllBytes(inputPath);
        int totalNeeded = STREAM_COUNT * HASHES_PER_STREAM * CHUNK_SIZE;

        if (allData.Length < totalNeeded)
        {
            Console.WriteLine("Not enough data to build all streams with full hash depth.");
            Console.WriteLine("Required: " + totalNeeded + " bytes | Found: " + allData.Length + " bytes");
            Pause();
            return;
        }

        using (FileStream outStream = new FileStream(outputPath, FileMode.Create))
        using (SHA256 sha = SHA256.Create())
        {
            for (int i = 0; i < STREAM_COUNT; i++)
            {
                for (int j = 0; j < HASHES_PER_STREAM; j++)
                {
                    int offset = (i * HASHES_PER_STREAM + j) * CHUNK_SIZE;
                    byte[] chunk = new byte[CHUNK_SIZE];
                    Array.Copy(allData, offset, chunk, 0, CHUNK_SIZE);

                    byte[] hash = sha.ComputeHash(chunk);
                    outStream.Write(hash, 0, hash.Length);
                }

                Console.WriteLine("Stream " + (i + 1) + " of " + STREAM_COUNT + " written.");
            }
        }

        Console.WriteLine("All streams processed and saved to: " + outputPath);
        Pause();
    }

    static void Pause()
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
