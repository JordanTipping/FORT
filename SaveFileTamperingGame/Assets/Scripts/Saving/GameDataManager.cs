using UnityEngine;
using System.IO;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    public Transform playerTransform;  //cylindrical player
    public Transform cameraTransform; 
    private GameData gameData; //data
    private string saveFilePath;


    private void Awake()
    {
        //ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {       
        gameData = new GameData();
        //current save path is pretty hard coded. 
        saveFilePath = Path.Combine(Application.persistentDataPath, "GameData.json");
        Debug.Log($"Save file path: {saveFilePath}");
    }

    private void PopulateGameData()
    {
        
        //player pos
        gameData.playerPosition[0] = playerTransform.position.x;
        gameData.playerPosition[1] = playerTransform.position.y;
        gameData.playerPosition[2] = playerTransform.position.z;

        // cam pos 
        gameData.cameraPosition[0] = cameraTransform.position.x;
        gameData.cameraPosition[1] = cameraTransform.position.y;
        gameData.cameraPosition[2] = cameraTransform.position.z;

        //cam rot
        gameData.cameraRotation[0] = cameraTransform.eulerAngles.x;
        gameData.cameraRotation[1] = cameraTransform.eulerAngles.y;
        gameData.cameraRotation[2] = cameraTransform.eulerAngles.z;
    }

    public void SaveGame()
    {
        PopulateGameData(); 

        //serializing 
        string json = JsonUtility.ToJson(gameData, true);

        //save to file 
        File.WriteAllText(saveFilePath, json);

        Debug.Log("Game saved successfully!");

        //clarity in testing 
        Debug.Log($"Saved Player Position: [{string.Join(", ", gameData.playerPosition)}]");
        Debug.Log($"Saved Camera Position: [{string.Join(", ", gameData.cameraPosition)}]");
        Debug.Log($"Saved Camera Rotation: [{string.Join(", ", gameData.cameraRotation)}]");

        //Dev Configurable. Notifies FORT that a file has been saved
        //
        //MANUAL TRIGGER that avoids watching the file directory. 

        Fort.Instance.NotifyFileSaved("GameData.json");

    }


    public async void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            Debug.Log("Save file found. Notifying FORT of load attempt...");

            bool integrityOk = await Fort.Instance.NotifyFileLoadAttempt("GameData.json");

            if (!integrityOk)
            {
                Debug.LogError("Load aborted due to failed integrity check.");
                return; // STOP HERE if tampered!
            }

            // Only runs if blockchain integrity passed.
            string json = File.ReadAllText(saveFilePath);
            gameData = JsonUtility.FromJson<GameData>(json);

            playerTransform.position = new Vector3(
                gameData.playerPosition[0],
                gameData.playerPosition[1],
                gameData.playerPosition[2]
            );

            cameraTransform.position = new Vector3(
                gameData.cameraPosition[0],
                gameData.cameraPosition[1],
                gameData.cameraPosition[2]
            );

            CameraOrbit cameraOrbit = cameraTransform.GetComponent<CameraOrbit>();
            if (cameraOrbit != null)
            {
                cameraOrbit.SetCameraRotation(new Vector3(
                    gameData.cameraRotation[0],
                    gameData.cameraRotation[1],
                    gameData.cameraRotation[2]
                ));
            }
            else
            {
                cameraTransform.eulerAngles = new Vector3(
                    gameData.cameraRotation[0],
                    gameData.cameraRotation[1],
                    gameData.cameraRotation[2]
                );
            }

            Debug.Log("Game loaded successfully!");
            Debug.Log($"Loaded Player Position: [{string.Join(", ", gameData.playerPosition)}]");
            Debug.Log($"Loaded Camera Position: [{string.Join(", ", gameData.cameraPosition)}]");
            Debug.Log($"Loaded Camera Rotation: [{string.Join(", ", gameData.cameraRotation)}]");
        }
        else
        {
            Debug.LogWarning("Save file not found! Cannot load game.");
        }
    }




}
