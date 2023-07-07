const { Events } = require("discord.js");

module.exports = {

    name: Events.botReady,
	once: true,
	execute(bot) {
    console.log(`Le bot discord : ${bot.user.tag} est en ligne`)
    }
}

