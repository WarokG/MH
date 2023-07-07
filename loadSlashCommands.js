const { token, botId } = require("./config.json");
const { REST, Routes } = require ("discord.js");
const fs = require("fs");
const path = require('node:path');
    
    const commands = [];
    const folderPath = path.join(__dirname, "Commands");

    fs.readdirSync("./Commands").filter(file => file.endsWith(".js")).forEach(async file => {

        // crée toutes les commandes du fichier
        const filePath = path.join(folderPath, file);
        const command = require(filePath);
        if ('data' in command && 'execute' in command) {
			commands.push(command.data.toJSON());
		} else {
			console.log(`[WARNING] The command at ${filePath} is missing a required "data" or "execute" property.`);
		}
    })

    const rest = new REST().setToken(token);

    (async () => {
        try {
            // charge les commandes
            const data = await rest.put(Routes.applicationCommands(botId),{ body: commands },);
    
            console.log(`Successfully reloaded ${data.length} application (/) commands.`);
        } catch (error) {
            console.error(error);
        }
    })();


