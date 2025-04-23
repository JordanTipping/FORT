[System.Serializable]


//serializable data to allow saving as JSON. 
public class GameData
{
    

    //arrays of floats for vector data seems weird but is necessary for serialization 
    public float[] playerPosition; 
    public float[] cameraPosition; 
    public float[] cameraRotation;

    
    public GameData()
    {

       
        playerPosition = new float[3]; 
        cameraPosition = new float[3];  
        cameraRotation = new float[3];  
    }
}
