using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Security.Cryptography;

public class ImpTests
{
    private GameObject testObject;
    private GanacheConnector ganacheConnector;
    private Fort fortInstance;

    [SetUp]
    public void Setup()
    {
        testObject = new GameObject("TestObject");
        ganacheConnector = testObject.AddComponent<GanacheConnector>();

        var fortObj = new GameObject("FortInstance");
        fortInstance = fortObj.AddComponent<Fort>();

        string testSaltFileName = "test_salt.txt";
        string productionSaltFileName = "salt.txt";
        string testSaltFilePath = Path.Combine(Application.persistentDataPath, testSaltFileName);
        string productionSaltFilePath = Path.Combine(Application.persistentDataPath, productionSaltFileName);

        if (!File.Exists(testSaltFilePath))
        {
            File.WriteAllText(testSaltFilePath, "dummySaltValue");
        }
        File.Copy(testSaltFilePath, productionSaltFilePath, true);
    }

    [TearDown]
    public void Teardown()
    {
        string[] filenames = new string[]
        {
            "test_salt.txt",
            "salt.txt"
        };

        foreach (string filename in filenames)
        {
            string filePath = Path.Combine(Application.persistentDataPath, filename);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        Object.Destroy(testObject);
        Object.Destroy(fortInstance.gameObject);
    }

    //Tests basic functionality of sending a save file hash to the blockchain.
    //Ensures that hash storage via the smart contract executes successfully.
    [UnityTest]
    public IEnumerator TestStoreHash()
    {
        yield return new WaitForSeconds(4f);

        string dummyHashedContent = "dummyHashedContentValue";

        var storeTask = ganacheConnector.SendHashToBlockchain(dummyHashedContent);
        while (!storeTask.IsCompleted) yield return null;

        string txHash = storeTask.Result;
        Debug.Log("StoreHash Transaction Response: " + txHash);

        Assert.IsNotNull(txHash, "Expected a transaction hash from storeHash.");
        yield break;
    }

    //Validates that a correct hash returns a successful load validation from the blockchain.
    //ritical to verify that legitimate save files pass integrity checks.
    [UnityTest]
    public IEnumerator TestValidateLoadPositive()
    {
        yield return new WaitForSeconds(4f);

        string dummyHashedContent = "dummyHashedContentValue";
        var storeTask = ganacheConnector.SendHashToBlockchain(dummyHashedContent);
        while (!storeTask.IsCompleted) yield return null;
        Assert.IsNotNull(storeTask.Result, "Store hash transaction returned null.");

        var validTask = ganacheConnector.ValidateLoad(dummyHashedContent);
        while (!validTask.IsCompleted) yield return null;
        string validResponse = validTask.Result;
        Debug.Log("ValidateLoad (positive) Response: " + validResponse);
        Assert.AreEqual("Load Match - File integrity verified.", validResponse,
            "Expected a valid load validation response.");

        yield break;
    }

    //Ensures that tampered hashes or incorrect hashes are properly rejected.
    [UnityTest]
    public IEnumerator TestValidateLoadNegative()
    {
        yield return new WaitForSeconds(4f);

        string dummyHashedContent = "dummyHashedContentValue";
        var storeTask = ganacheConnector.SendHashToBlockchain(dummyHashedContent);
        while (!storeTask.IsCompleted) yield return null;
        Assert.IsNotNull(storeTask.Result, "Store hash transaction returned null.");

        LogAssert.Expect(LogType.Error, "Unrecognized Hash - This file has not been seen before.");

        var invalidTask = ganacheConnector.ValidateLoad("wrongDummyHashValue");
        while (!invalidTask.IsCompleted) yield return null;
        string invalidResponse = invalidTask.Result;
        Debug.Log("ValidateLoad (negative) Response: " + invalidResponse);
        Assert.IsTrue(invalidResponse.Contains("Unrecognized Hash"),
            "Expected a failure message for unrecognized hash.");

        yield break;
    }


    //Attempts to simulate a replay attack by reusing a previously valid hash
    //on a tampered save file. Ensures tampering AND replay protection are both working.
    [UnityTest]
    public IEnumerator TestWasReplayAttackBlocked()
    {
        yield return new WaitForSeconds(4f);

        string filePath = Path.Combine(Application.persistentDataPath, "GameData.json");

        string originalContents = "{ \"playerPosition\": [0.0, 0.0, 0.0], \"cameraPosition\": [0.0, 0.0, 0.0], \"cameraRotation\": [0.0, 0.0, 0.0] }";
        File.WriteAllText(filePath, originalContents);
        string validHash = Fort.Instance.HashFileContents(originalContents);
        yield return ganacheConnector.SendHashToBlockchain(validHash);
        Debug.Log("Step 1: Valid save stored on blockchain.");

        string tamperedContents = "{ \"playerPosition\": [999.0, 999.0, 999.0], \"cameraPosition\": [0.0, 0.0, 0.0], \"cameraRotation\": [0.0, 0.0, 0.0] }";
        File.WriteAllText(filePath, tamperedContents);
        Debug.Log("Step 2: Save file tampered locally.");

        LogAssert.Expect(LogType.Error, "Tampering detected. Secure wipe triggered.");

        Debug.Log("Step 3: Attempting replay attack with old hash against tampered file...");
        var replayTask = ganacheConnector.ValidateLoad(validHash);
        while (!replayTask.IsCompleted) yield return null;

        string response = replayTask.Result;
        Debug.Log("Replay attack blockchain response: " + response);

        bool isReplayDetected = response.Contains("Replay Attack Detected");
        bool isTamperDetected = response.Contains("Load Validation Failed - Possible tampering detected.");
        Assert.IsTrue(isReplayDetected || isTamperDetected, "Replay or tampering protection should have blocked the attack.");

        yield break;
    }

    //Verifies that the blockchain genesis block matches the default zero-state save.
    //Ensures that the game starts from a secure, untampered state.
    [UnityTest]
    public IEnumerator TestGenesisBlockValidationOfDefaultZeroState()
    {
        yield return new WaitForSeconds(4f);

        string unityZeroState = "{ \"playerPosition\": [0.0, 0.0, 0.0], \"cameraPosition\": [0.0, 0.0, 0.0], \"cameraRotation\": [0.0, 0.0, 0.0] }";

        byte[] utf8Bytes = new UTF8Encoding(false).GetBytes(unityZeroState);

        byte[] hashBytes;
        using (SHA256 sha256 = SHA256.Create())
        {
            hashBytes = sha256.ComputeHash(utf8Bytes);
        }

        var validateTask = ganacheConnector.ValidateGenesisDirect(hashBytes);
        while (!validateTask.IsCompleted) yield return null;

        string validationResponse = validateTask.Result;
        Debug.Log("Genesis Validation Response: " + validationResponse);

        Assert.AreEqual("Genesis Match - Initial state is trusted. Good to go!", validationResponse,
            "Expected the genesis block to match Unity's default state.");

        yield break;
    }

    //TestWasRaceConditionBlocked: Simulates a race condition by sending two different hashes quickly.
    //Ensures only the last stored hash is valid and older hashes get rejected.
    [UnityTest]


    public IEnumerator TestWasRaceConditionBlocked()
    {
        yield return new WaitForSeconds(4f);

        string hash1 = "raceHashOne";
        string hash2 = "raceHashTwo";

        Debug.Log("Submitting hash1...");
        var store1 = ganacheConnector.SendHashToBlockchain(hash1);
        while (!store1.IsCompleted) yield return null;

        Debug.Log("Submitting hash2 immediately after...");
        var store2 = ganacheConnector.SendHashToBlockchain(hash2);
        while (!store2.IsCompleted) yield return null;

        Debug.Log("Validating hash1 (should be rejected now)...");
        LogAssert.Expect(LogType.Error, "Tampering detected. Secure wipe triggered.");
        var validate1 = ganacheConnector.ValidateLoad(hash1);
        while (!validate1.IsCompleted) yield return null;

        Debug.Log("Validation result for hash1: " + validate1.Result);
        Assert.IsTrue(validate1.Result.Contains("Load Validation Failed"));
        yield break;
    }

    //TestWasRandomHashForged: Attempts to validate a random forged hash.
    //Ensures blockchain rejects unknown hashes and triggers replay/tamper protection.
    [UnityTest]
    public IEnumerator TestWasRandomHashForged()
    {
        yield return new WaitForSeconds(4f);

        string forgedHash = "thisIsAFakeHashAttempt";
        Debug.Log("Forged hash: " + forgedHash);

        Debug.Log("Validating forged hash...");
        LogAssert.Expect(LogType.Error, "Unrecognized Hash - This file has not been seen before.");

        var forgedValidate = ganacheConnector.ValidateLoad(forgedHash);
        while (!forgedValidate.IsCompleted) yield return null;

        Debug.Log("Validation result for forged hash: " + forgedValidate.Result);
        Assert.IsTrue(forgedValidate.Result.Contains("Unrecognized Hash"),
            "Expected a failure message for unrecognized hash.");




        yield break;
    }


    //TestWasRepeatedSubmissionBlocked: Confirms the SAME valid hash can be reused safely (expected behavior).
    [UnityTest]
    public IEnumerator TestWasRepeatedSubmissionBlocked()
    {
        yield return new WaitForSeconds(4f);

        string dummyHashedContent = "dummySafeHash";
        Debug.Log("Sending dummy valid hash...");
        var storeTask = ganacheConnector.SendHashToBlockchain(dummyHashedContent);
        while (!storeTask.IsCompleted) yield return null;

        Debug.Log("Re-sending same hash (allowed)...");
        var storeAgainTask = ganacheConnector.SendHashToBlockchain(dummyHashedContent);
        while (!storeAgainTask.IsCompleted) yield return null;

        Debug.Log("Validating dummy hash (should still be valid)...");
        var validateTask = ganacheConnector.ValidateLoad(dummyHashedContent);
        while (!validateTask.IsCompleted) yield return null;

        Debug.Log("Validation result for repeated submission: " + validateTask.Result);
        Assert.AreEqual("Load Match - File integrity verified.", validateTask.Result);

        yield break;
    }


    //estWasFakeGenesisSent: Simulates submitting a random genesis hash to validation.
    //verifies the genesis validation can’t be spoofed by fake hashes.
    [UnityTest]
    public IEnumerator TestWasFakeGenesisSent()
    {
        yield return new WaitForSeconds(4f);

        //Generate a nonsense genesis hash
        byte[] fakeGenesis = Encoding.UTF8.GetBytes("FAKE_GENESIS_HASH");
        using (SHA256 sha256 = SHA256.Create())
        {
            fakeGenesis = sha256.ComputeHash(fakeGenesis);
        }

        var validateTask = ganacheConnector.ValidateGenesisDirect(fakeGenesis);
        while (!validateTask.IsCompleted) yield return null;

        string response = validateTask.Result;
        Debug.Log($"Fake Genesis Validation Response: {response}");
        Assert.AreEqual("Genesis Validation Failed - Possible tampering detected.", response);

        yield break;
    }


}
