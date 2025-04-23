using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Nethereum.Web3;
using Nethereum.Contracts;
using System.Security.Cryptography;

// This script utilises Nethereum to interact with a local Ethereum testnet blockchain.
// Guidance was taken from:
// https://docs.nethereum.com/en/latest/nethereum-gettingstarted-smartcontracts-untyped/

public class GanacheConnector : MonoBehaviour
{
    public Web3 web3; 


    // When a new contract is deployed, the contract address and ABI must be fetched. 
    // Blockchain/Build/Contracts = Contains The Contract JSON, which within has the ABI. Wallet address must also be changed in: CreateGenesisHash() and SendHashToBlockchain()
    private string contractAddress = "0xe72F4435f126690bf9824d809286d34d80FC09d3";
    public Contract contract;

    private string abi = @"[
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": false,
          ""name"": ""genesisHash"",
          ""type"": ""bytes32""
        }
      ],
      ""name"": ""GenesisCreated"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": false,
          ""name"": ""message"",
          ""type"": ""string""
        }
      ],
      ""name"": ""GenesisValidationSuccess"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": false,
          ""name"": ""message"",
          ""type"": ""string""
        }
      ],
      ""name"": ""GenesisValidationFailure"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": false,
          ""name"": ""message"",
          ""type"": ""string""
        }
      ],
      ""name"": ""LoadValidationSuccess"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": false,
          ""name"": ""message"",
          ""type"": ""string""
        }
      ],
      ""name"": ""LoadValidationFailure"",
      ""type"": ""event""
    },
    {
      ""constant"": false,
      ""inputs"": [
        {
          ""name"": ""_hash"",
          ""type"": ""string""
        },
        {
          ""name"": ""_playerSalt"",
          ""type"": ""string""
        }
      ],
      ""name"": ""storeHash"",
      ""outputs"": [],
      ""payable"": false,
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""constant"": true,
      ""inputs"": [],
      ""name"": ""getPlayerSaltHash"",
      ""outputs"": [
        {
          ""name"": """",
          ""type"": ""bytes32""
        }
      ],
      ""payable"": false,
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""constant"": true,
      ""inputs"": [],
      ""name"": ""getHash"",
      ""outputs"": [
        {
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""payable"": false,
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""constant"": false,
      ""inputs"": [],
      ""name"": ""createGenesis"",
      ""outputs"": [],
      ""payable"": false,
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""constant"": true,
      ""inputs"": [],
      ""name"": ""getGenesisHash"",
      ""outputs"": [
        {
          ""name"": """",
          ""type"": ""bytes32""
        }
      ],
      ""payable"": false,
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""constant"": false,
      ""inputs"": [
        {
          ""name"": ""_incomingHash"",
          ""type"": ""bytes32""
        }
      ],
      ""name"": ""validateGenesis"",
      ""outputs"": [
        {
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""payable"": false,
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""constant"": false,
      ""inputs"": [
        {
          ""name"": ""_incomingHash"",
          ""type"": ""string""
        }
      ],
      ""name"": ""validateLoad"",
      ""outputs"": [
        {
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""payable"": false,
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    }
  ]"; 

    public async void Start()
    {
        string rpcUrl = "http://127.0.0.1:7545";

        Debug.Log("Attempting to connect...");
        web3 = new Web3(rpcUrl);

        try
        {
            var blockNumberTask = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            await Task.WhenAny(blockNumberTask, Task.Delay(3000));

            if (blockNumberTask.IsCompletedSuccessfully)
            {
                Debug.Log($"Blockchain connected! Latest Block: {blockNumberTask.Result.Value}");
                contract = web3.Eth.GetContract(abi, contractAddress);
                Debug.Log("Contract loaded successfully.");
            }
            else
            {
                Debug.LogWarning("Ganache did not respond in time. Blockchain functions will be unavailable.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not connect to Ganache: {ex.Message}. Blockchain functions will be unavailable.");
        }
    }

    public async Task CreateGenesisBlock()
    {
        Debug.Log("New Game detected - Call to create Genesis Block");

        try
        {
            var createGenesisFunction = contract.GetFunction("createGenesis");
            var transactionHash = await createGenesisFunction.SendTransactionAsync(
                from: "0x55F6ED9E0190aD3EaBe476dc3FbAF76640b29C31",
                gas: new Nethereum.Hex.HexTypes.HexBigInteger(3000000),
                value: new Nethereum.Hex.HexTypes.HexBigInteger(0)
            );

            Debug.Log($"Genesis block creation transaction sent. TX: {transactionHash}");
            await Task.Delay(5000);
            await SendGenesisHashForValidation();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Genesis function issue: {ex.Message}");
        }
    }

    public async Task SendGenesisHashForValidation()
    {
        string genesisHashFilePath = Path.Combine(Application.persistentDataPath, "GameData_genesis.hash");

        if (!File.Exists(genesisHashFilePath))
        {
            Debug.LogError("Genesis file not present for validation request...");
            return;
        }

        byte[] localGenesisHashBytes = File.ReadAllBytes(genesisHashFilePath);
        if (localGenesisHashBytes.Length != 32)
        {
            Debug.LogError($"Invalid hash length: {localGenesisHashBytes.Length} bytes. Expected 32 bytes.");
            return;
        }

        string hexHash = BitConverter.ToString(localGenesisHashBytes).Replace("-", "").ToLower();
        Debug.Log($"Local Genesis Hash (Before Validation): {hexHash}");

        try
        {
            var validateGenesisFunction = contract.GetFunction("validateGenesis");
            string validationResponse = await validateGenesisFunction.CallAsync<string>(localGenesisHashBytes);
            Debug.Log($"Blockchain Response: {validationResponse}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to validate Genesis: {ex.Message}");
        }
    }

    public async Task<string> ValidateGenesisDirect(byte[] incomingHashBytes)
    {
        try
        {
            var validateGenesisFunction = contract.GetFunction("validateGenesis");
            string result = await validateGenesisFunction.CallAsync<string>(incomingHashBytes);
            Debug.Log("ValidateGenesisDirect result: " + result);
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error in ValidateGenesisDirect: " + ex.Message);
            return null;
        }
    }

    public async Task<string> ValidateLoad(string hashedLoadFile)
    {
        Debug.Log($"Sending Load Hash for Validation: {hashedLoadFile}");
        string validationResponse = "";
        try
        {
            var validateLoadFunction = contract.GetFunction("validateLoad");
            validationResponse = await validateLoadFunction.CallAsync<string>(hashedLoadFile);
            Debug.Log($"Blockchain Response: {validationResponse}");

            if (validationResponse.Contains("Replay Attack Detected"))
            {
                Debug.LogError("Replay attack detected. Aborting load + secure wipe.");
                string filePath = Path.Combine(Application.persistentDataPath, "GameData.json");
                SecureEncryptAndDeleteFile(filePath);
            }
            else if (validationResponse.Contains("Load Validation Failed"))
            {
                Debug.LogError("Tampering detected. Secure wipe triggered.");
                string filePath = Path.Combine(Application.persistentDataPath, "GameData.json");
                SecureEncryptAndDeleteFile(filePath);
            }
            else if (validationResponse.Contains("Load Match"))
            {
                Debug.Log("Load validated successfully.");
            }
            else if (validationResponse.Contains("Unrecognized Hash"))
            {
                Debug.LogError("Unrecognized Hash - This file has not been seen before.");
                string filePath = Path.Combine(Application.persistentDataPath, "GameData.json");
                SecureEncryptAndDeleteFile(filePath);
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to validate Load: {ex.Message}");
            validationResponse = "Validation Exception: " + ex.Message;
        }
        return validationResponse;
    }


    public async Task<string> SendHashToBlockchain(string hashedContent)
    {
        Debug.Log("Preparing to send hash and salt to blockchain...");
        try
        {
            var storeHashFunction = contract.GetFunction("storeHash");
            Debug.Log("storeHash function found.");
            string playerSalt = File.ReadAllText(Path.Combine(Application.persistentDataPath, "salt.txt")).Trim();
            Debug.Log($"Sending transaction with hash: {hashedContent} and salt: {playerSalt}");

            var transactionHash = await storeHashFunction.SendTransactionAsync(
                from: "0x55F6ED9E0190aD3EaBe476dc3FbAF76640b29C31",
                gas: new Nethereum.Hex.HexTypes.HexBigInteger(3000000),
                value: new Nethereum.Hex.HexTypes.HexBigInteger(0),
                functionInput: new object[] { hashedContent, playerSalt }
            );

            Debug.Log($"Hash and salt sent to blockchain successfully. Transaction hash: {transactionHash}");
            return transactionHash;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to send hash and salt to blockchain: {ex.Message}");
            return null;
        }
    }

    public void SecureEncryptAndDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                byte[] fileContent = File.ReadAllBytes(filePath);
                byte[] key = new byte[32];
                byte[] iv = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(key);
                    rng.GetBytes(iv);
                }

                byte[] encryptedData;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(fileContent, 0, fileContent.Length);
                        }
                        encryptedData = msEncrypt.ToArray();
                    }
                }

                File.WriteAllBytes(filePath, encryptedData);
                File.Delete(filePath);
            }
            else
            {
                Debug.LogWarning("File not found for secure encryption and deletion.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error during secure encryption and deletion: " + ex.Message);
        }
    }
}
