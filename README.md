# DiscordPromptsBot
A bot that sends prompts in a Discord thread every time it starts.

## Use
* Create a Discord bot on the [Developer Portal](https://discord.com/developers/) 
and make it join your server.
* Using the `appsettings.json.dist` file as a template, create the 
`appsettings.json` file and input the correct parameters:
	* The token for your bot
	* The ID of the thread the bot will post in
	* The ID of a role for the bot to ping
	* The list of prompts that the bot can post
* Start the application
* The bot should post its first prompt!
 
This application only posts once, and then closes. If you want to post prompts at 
a regular interval, run it with a task scheduler (`cron`, the Windows task scheduler, ...)

This was written as a training project for a few friends, so the code is a bit simplistic and definitely contains bugs. Beware.