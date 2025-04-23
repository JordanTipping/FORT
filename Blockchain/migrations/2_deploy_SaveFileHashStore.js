const SaveFileHashStore = artifacts.require("SaveFileHashStore");

module.exports = function (deployer) {
    deployer.deploy(SaveFileHashStore);
};
