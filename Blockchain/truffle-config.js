/**
 * Truffle configuration file for managing networks, compilers, and other settings.
 * For more information, visit: https://trufflesuite.com/docs/truffle/reference/configuration
 */

module.exports = {
    networks: {
        // Development network for Ganache
        development: {
            host: "127.0.0.1", // Ganache default RPC URL
            port: 7545,        // Ganache default port
            network_id: "*",   // Match any network ID (wildcard or use 1337 for Ganache)
        },
    },

    // Compiler settings
    compilers: {
        solc: {
            version: "0.5.1", // Specify the Solidity version to use
        },
    },

    // Additional settings (if needed later)
    mocha: {
        // timeout: 100000, // Increase if tests are taking too long
    },

    // Optional: Truffle DB (disabled by default)
    // db: {
    //   enabled: false,
    // },
};
