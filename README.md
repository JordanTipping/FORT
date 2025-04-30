# FORT – Framework for On-Chain Robust Tamper Detection

A prototype artefact for securing game save files using blockchain-based validation, layered cryptographic hashing, and player-derived entropy.

---

## Overview

FORT is a Unity-integrated anti-tamper framework using:

- Blockchain validation (Ganache + Truffle)
- Player behaviour as a cryptographic salt

---


## Setup

### 1. Clone the repo

Via GUI or git CLI

### 2. Set up Ganache (GUI version recommended)

1. Download Ganache: https://trufflesuite.com/ganache
Documentation: https://archive.trufflesuite.com/docs/ganache/quickstart/

2. Launch the **Ganache GUI**
3. Click **New Workspace - Ethereum**
4. Got to Chain and set:
   - **Gas Limit**: `12000000`
   - **Gas Price**: `0`
5. Save and launch the workspace
6. Make note RPC Server URL, usually http://127.0.0.1:7545, which is already set in GanacheConnector Script. 

---

### 3. Set up Truffle 

From inside the `Blockchain/` folder:

```bash
npm install -g truffle         
truffle init                   
```

This will download the truffle dev environment and create your Truffle-Config.js file - Go to Ganache GUI - Your workspace - Settings - Point Truffle Project at your JS file. (The folders that truffle init usually creates, and the truffle-config.js are already present in the repo. This step is still necessary to install truffle.)

Check that the Solidity Smart Contract `SaveFileHashStore.sol` is in `Blockchain/contracts/`

Then, again within the Blockchain/ folder compile and deploy to Ganache:

```bash
truffle compile
truffle migrate --reset
```

Check Ganache GUI under **contracts**, should say **Deployed**

---

## Unity Game Setup

1. Open the project in **Unity 2021.3+**
2. Go to Assets - Open GanacheConnector.cs script. 
	-  2.1. Open Ganache GUI, go to contracts. Copy contract address into Contract Variable in GanacheConnector.
	-  2.2  Open SaveFileHashStore.json. Take ABI variable, copy long value in []. Go to GanacheConnector - Replace ABI. 
	-  2.3  Open Ganache GUI, go to accounts - Copy a wallet address - Replace from: in CreateGenesisHash() and 					SendHashToBlockchain() with this wallet address. 


3. Load the sample scene and press Play. Keep an eye on console logs. 
4. Use WASD to move the player
5. Use the pause menu[P] to Save and Load game state
6. Blockchain validation occurs on save/load behind the scenes

Go to Unity's persistent data path (e.g: C:\Users\YOUR USERNAME\AppData\LocalLow\DefaultCompany\SaveFileTamperingGame)

From here, you can test changing the GameData.JSON, and loading a tampered file from Unity. The blockchain validation will prevent this from happening. 


---

NIST Suite is included aswell as Accumulated_salt.txt, and a post processing utility built to make the entropic data usable. 

## Project Demo - Including deploying contract. 

https://uweacuk-my.sharepoint.com/:v:/g/personal/jordan2_tipping_live_uwe_ac_uk/ETrYM7JydS5NhjQRIwJAgXwB1gZOj2jRr2nJXFVSVfZn8A?nav=eyJyZWZlcnJhbEluZm8iOnsicmVmZXJyYWxBcHAiOiJPbmVEcml2ZUZvckJ1c2luZXNzIiwicmVmZXJyYWxBcHBQbGF0Zm9ybSI6IldlYiIsInJlZmVycmFsTW9kZSI6InZpZXciLCJyZWZlcnJhbFZpZXciOiJNeUZpbGVzTGlua0NvcHkifX0&e=700OgP

