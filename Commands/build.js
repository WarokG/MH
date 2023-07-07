const Discord = require("discord.js");
const { SlashCommandBuilder } = require('discord.js');

module.exports = {

    data: new SlashCommandBuilder()
		.setName('build')
		.setDescription('Donne le build voulu'),

    async execute(interaction) {

        await interaction.reply(`Kamoulox!`);
    },

}