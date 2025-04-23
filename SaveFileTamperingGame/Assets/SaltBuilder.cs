using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class SaltBuilder : MonoBehaviour
{
    private string saltFilePath;
    private string accumulatedSaltFilePath;
    private GameDataManager gameDataManager;
    private float updateInterval = 0.001f;
    private const int BITSTREAM_COUNT = 100;
    private const int BIT_TARGET = 500000 * BITSTREAM_COUNT;
    private const int BYTE_TARGET = BIT_TARGET / 8;

    private void Start()
    {
        saltFilePath = Path.Combine(Application.persistentDataPath, "salt.txt");
        accumulatedSaltFilePath = Path.Combine(Application.persistentDataPath, "accumulated_salt.txt");
        gameDataManager = FindObjectOfType<GameDataManager>();

        if (gameDataManager == null)
        {
            Debug.LogError("SaltBuilder: No GameDataManager found in the scene.");
            return;
        }

        Debug.Log($"SaltBuilder: Booted. Writing to {accumulatedSaltFilePath}");
        StartCoroutine(GenerateSaltCoroutine());
        StartCoroutine(AccumulateSaltForNIST());
    }

    private IEnumerator GenerateSaltCoroutine()
    {
        while (true)
        {
            WriteSaltToFile();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private IEnumerator AccumulateSaltForNIST()
    {
        while (true)
        {
            AccumulateSalt();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void WriteSaltToFile()
    {
        if (gameDataManager == null) return;

        gameDataManager.Invoke("PopulateGameData", 0f);

        GameData gameData = gameDataManager.GetComponent<GameDataManager>().GetType()
            .GetField("gameData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(gameDataManager) as GameData;

        if (gameData == null) return;

        List<float> values = new List<float>();
        values.AddRange(gameData.playerPosition);
        values.AddRange(gameData.cameraPosition);
        values.AddRange(gameData.cameraRotation);

        ShuffleList(values);
        string saltData = string.Join(",", values);

        try
        {
            File.WriteAllText(saltFilePath, saltData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaltBuilder: Failed to write salt file - {ex.Message}");
        }
    }

    private void AccumulateSalt()
    {
        if (gameDataManager == null) return;

        gameDataManager.Invoke("PopulateGameData", 0f);

        GameData gameData = gameDataManager.GetComponent<GameDataManager>().GetType()
            .GetField("gameData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(gameDataManager) as GameData;

        if (gameData == null) return;

        List<float> values = new List<float>();
        values.AddRange(gameData.playerPosition);
        values.AddRange(gameData.cameraPosition);
        values.AddRange(gameData.cameraRotation);

        ShuffleList(values);
        byte[] hashedSalt = ConvertToHashedBinary(values);

        try
        {
            using (FileStream fs = new FileStream(accumulatedSaltFilePath, FileMode.Append, FileAccess.Write))
            {
                fs.Write(hashedSalt, 0, hashedSalt.Length);
            }

            long fileSizeBytes = new FileInfo(accumulatedSaltFilePath).Length;
            int fullStreams = (int)(fileSizeBytes / 62500);

            //Debug.Log($"[Salt Accumulation] {fileSizeBytes} bytes written | {fullStreams} / {BITSTREAM_COUNT} streams complete");

            if (fileSizeBytes >= BYTE_TARGET)
            {
                //Debug.Log($"NIST accumulation complete: {fileSizeBytes} bytes written = {BIT_TARGET} bits total.");
                StopCoroutine(AccumulateSaltForNIST());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaltBuilder: Failed to accumulate salt - {ex.Message}");
        }
    }

   
    private byte[] ConvertToHashedBinary(List<float> values)
    {
        using (SHA256 sha = SHA256.Create())
        {
            List<byte> result = new List<byte>();

            foreach (float val in values)
            {
                byte[] floatBytes = BitConverter.GetBytes(val);
                byte[] hash = sha.ComputeHash(floatBytes);

               
                result.AddRange(new ArraySegment<byte>(hash, 0, 4));
            }

            return result.ToArray();
        }
    }

    private void ShuffleList(List<float> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}
