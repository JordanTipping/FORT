using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


//This script uses SHA-256 hashing 
//based on the .NET implementation of System.Security.Cryptography.SHA256:
//https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256?view=net-9.0

public class Fort : MonoBehaviour
{
    private string saveDirectory;
    private string saveFilePath;
    private string genesisFilePath;
    private string genesisHashFilePath;

    public GanacheConnector ganacheConnector;

    public static Fort Instance { get; private set; }

    public void SetGanacheConnector(GanacheConnector connector)
    {
        ganacheConnector = connector;
    }

    private void Awake()
    {
     
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
        SetSaveDirectory(Application.persistentDataPath);

        if (ganacheConnector == null)
        {
            ganacheConnector = FindObjectOfType<GanacheConnector>();
        }

        // Wait until GanacheConnector is connected. Succeeds if Ganache is running and contract is deployed. 
        while (ganacheConnector.web3 == null || ganacheConnector.contract == null)
        {
            Debug.Log("Waiting for GanacheConnector...");
            await Task.Delay(500);
        }

        Debug.Log("GanacheConnector ready. Proceeding with Fort startup logic.");
        CaptureGenesisDataOnStart();
    }



    public void SetSaveDirectory(string directory)
    {
        saveDirectory = directory;
        saveFilePath = Path.Combine(saveDirectory, "GameData.json");
        genesisFilePath = Path.Combine(saveDirectory, "GameData_genesis.json");
        genesisHashFilePath = Path.Combine(saveDirectory, "GameData_genesis.hash");
    }


    
    public void NotifyFileSaved(string fileName)
    {
        if (fileName == "GameData.json")
        {
            Debug.Log($"Manual trigger received for file: {fileName}");
            ProcessSaveFile();
        }
    }

    public async Task<bool> NotifyFileLoadAttempt(string fileName)
    {
        if (fileName == "GameData.json")
        {
            Debug.Log($"FORT notified: Load attempt detected for {fileName}");

            if (!File.Exists(saveFilePath))
            {
                Debug.LogError("Load failed: Save file does not exist!");
                return false;
            }

            string fileContents = File.ReadAllText(saveFilePath);

            // same hash process as performed on save
            string hashedLoadFile = HashFileContents(fileContents);
            Debug.Log($"Hashed Load File Contents: {hashedLoadFile}");

            // send to blockchain
            string validationResponse = await ganacheConnector.ValidateLoad(hashedLoadFile);

            // Results
            if (validationResponse == "Load Validation Failed - Possible tampering detected.")
            {
                Debug.LogError("Tampering detected; load aborted.");
                return false;
            }
            else if (validationResponse == "Load Match - File integrity verified.")
            {
                Debug.Log("Load validated successfully.");
                return true;
            }
            else
            {
                Debug.LogError($"Unexpected blockchain response: {validationResponse}");
                return false;
            }
        }

        return false;
    }

    private async void ProcessSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            Debug.Log("Save file found. Processing...");

            try
            {
                string fileContents = File.ReadAllText(saveFilePath);       
                string hashedContents = HashFileContents(fileContents);   

                Debug.Log($"Hashed save file contents: {hashedContents}");

                // Decoupled blockchain communication
                if (ganacheConnector != null)
                {
                    await HandleBlockchainCommunication(hashedContents);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error processing: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Save file not found during processing!");
        }
    }

    private async void CaptureGenesisDataOnStart()
    {
        if (!File.Exists(genesisFilePath)) //No Genesis file exists
        {
            Debug.Log("Genesis file not found. Capturing initial game data...");

            FreezeInput(true);

            try
            {
                // Default zero-state game data (MUST match Solidity for Correct Validation. Not Production ready:
                string initialGameState = "{ \"playerPosition\": [0.0, 0.0, 0.0], \"cameraPosition\": [0.0, 0.0, 0.0], \"cameraRotation\": [0.0, 0.0, 0.0] }";

                Debug.Log($"Generated Zero-State JSON: {initialGameState}");

                File.WriteAllText(genesisFilePath, initialGameState);
                Debug.Log($"Genesis file created at: {genesisFilePath}");


                byte[] binaryGenesisHash = HashFileContentsBinary(initialGameState);
                File.WriteAllBytes(genesisHashFilePath, binaryGenesisHash);

                string hexGenesisHash = BitConverter.ToString(binaryGenesisHash).Replace("-", "").ToLower();

                Debug.Log($"Genesis Binary Hash Stored: {BitConverter.ToString(binaryGenesisHash)}");
                Debug.Log($"Genesis Hex Hash: {hexGenesisHash}");

                //creating genesis block is done by asking the chain to do it, with a default state stored on chain - provided by Third Party Game Developer. 
                if (ganacheConnector != null)
                {
                    Debug.Log("Genesis hash created. Requesting blockchain genesis block creation...");
                    await ganacheConnector.CreateGenesisBlock();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error creating genesis file: {ex.Message}");
            }
            finally
            {
                FreezeInput(false);
            }
        }
        else
        {
            Debug.Log("Genesis file already exists. Skipping capture.");
        }
    }


    private byte[] HashFileContentsBinary(string fileContents)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(fileContents));
        }
    }


    public string HashFileContents(string fileContents)
    {
      
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(fileContents));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

          
            return builder.ToString();
        }
    }


    private async Task HandleBlockchainCommunication(string hashedContents)
    {
        try
        {
            string transactionHash = await ganacheConnector.SendHashToBlockchain(hashedContents);
            Debug.Log($"Transaction hash: {transactionHash}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error sending hash to blockchain: {ex.Message}");
        }
    }

    private void FreezeInput(bool freeze)
    {
      
        Debug.Log($"Input {(freeze ? "frozen" : "unfrozen")}.");
    }
}
