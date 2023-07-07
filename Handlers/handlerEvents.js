const Discord = require("discord.js");
const fs = require("fs");
const path = require('node:path');

module.exports = async (bot) => {

    const eventPath = path.join(__dirname, "..", "Events");
    const eventFiles = fs.readdirSync("./Events").filter(file => file.endsWith(".js"));

    for (const file of eventFiles) {
        const eventFilePath = path.join(eventPath, file);
        const event = require(eventFilePath);

        bot.on(file.split(".js").join(""), (...args) => event.execute(...args))

        console.log(`event ${file} est chargé`)
    }
}
