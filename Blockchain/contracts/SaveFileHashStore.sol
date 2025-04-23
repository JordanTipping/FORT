


contract SaveFileHashStore {
    string private fileHash;      
    bytes32 private genesisHash;   
    bytes32 private playerSaltHash; 
    mapping(string => bool) private usedHashes; 

    function storeHash(string memory _hash, string memory _playerSalt) public {
        if (playerSaltHash == 0) {
            playerSaltHash = keccak256(abi.encodePacked(_playerSalt));
        }

        
        bytes32 hashed = keccak256(abi.encodePacked(_hash, "123", playerSaltHash));
        fileHash = string(abi.encodePacked(hashed));

       
        usedHashes[fileHash] = true;
    }

    function getPlayerSaltHash() public view returns (bytes32) {
        return playerSaltHash;
    }

    function getHash() public view returns (string memory) {
        return fileHash;
    }

    function createGenesis() public {
        string memory zeroState = "{ \"playerPosition\": [0.0, 0.0, 0.0], \"cameraPosition\": [0.0, 0.0, 0.0], \"cameraRotation\": [0.0, 0.0, 0.0] }";
        bytes32 hashedGenesis = sha256(abi.encodePacked(zeroState));
        genesisHash = keccak256(abi.encodePacked(hashedGenesis, "123"));
        emit GenesisCreated(genesisHash);
    }

    function getGenesisHash() public view returns (bytes32) {
        return genesisHash;
    }

    function validateGenesis(bytes32 _incomingHash) public returns (string memory) {
        bytes32 computedSaltedHash = keccak256(abi.encodePacked(_incomingHash, "123"));
        if (computedSaltedHash == genesisHash) {
            emit GenesisValidationSuccess("Genesis Match - Initial state is trusted. Good to go!");
            return "Genesis Match - Initial state is trusted. Good to go!";
        } else {
            emit GenesisValidationFailure("Genesis Validation Failed - Possible tampering detected.");
            return "Genesis Validation Failed - Possible tampering detected.";
        }
    }

    function validateLoad(string memory _incomingHash) public returns (string memory) {
        require(playerSaltHash != 0, "No player salt stored. Validation impossible.");

        bytes32 computedSaveHash = keccak256(abi.encodePacked(_incomingHash, "123", playerSaltHash));
        string memory computedHashString = string(abi.encodePacked(computedSaveHash));

     
        if (!usedHashes[computedHashString]) {
            emit LoadValidationFailure("Unrecognized Hash - This file has not been seen before.");
            return "Unrecognized Hash - This file has not been seen before.";
        }

        //confirm integrity
        if (keccak256(abi.encodePacked(fileHash)) == keccak256(abi.encodePacked(computedHashString))) {
            emit LoadValidationSuccess("Load Match - File integrity verified.");
            return "Load Match - File integrity verified.";
        } else {
            emit LoadValidationFailure("Load Validation Failed - Possible tampering detected.");
            return "Load Validation Failed - Possible tampering detected.";
        }
    }

  
    event GenesisCreated(bytes32 genesisHash);
    event GenesisValidationSuccess(string message);
    event GenesisValidationFailure(string message);
    event LoadValidationSuccess(string message);
    event LoadValidationFailure(string message);
}
