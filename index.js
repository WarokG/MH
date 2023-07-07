const { Client, Collection, Events} = require("discord.js");
const bot = new Client({intents: 3276799});
const { token, botId } = require("./config.json");
const handlerEvents = require ("./Handlers/handlerEvents");
const fs = require("fs");
const path = require('node:path');

bot.commands = new Collection();
const folderPath = path.join(__dirname, "Commands");

fs.readdirSync("./Commands").filter(file => file.endsWith(".js")).forEach(async file => {

    // crée toutes les commandes du fichier
    const filePath = path.join(folderPath, file);
    const command = require(filePath);
		if ('data' in command && 'execute' in command) {
			bot.commands.set(command.data.name, command);
		} else {
			console.log(`[WARNING] La commande ${filePath} n'est pas complète il manque une "data" ou "execute" property.`);
		}
	}
)

handlerEvents(bot);
bot.login(token)





