# Discord prdelka-bot - C# DSharpPlus

Discord bot used for friends' Discord server.

It can do couple things.

- Mainly it has function to play story game, where each player knows only previous sentence and tries to continue in story. In the end, it can publish the story to grav website.

- It also can trigger certain "fake" person when it gets mentioned, answering with random quote.

## Why is this public

Honestly it is mostly worthles for most people, but some can take inspiration from this project.  
Story game is made quite well, rest is just for the memes.  
There won't really be more updates as we are currently rewriting bot in Node.js

## Building

dotnetcore required

Run update_and_rebuild.sh

```bash
#!/bin/bash
sudo systemctl stop prdelbot.service
git pull
sudo dotnet restore
sudo dotnet publish --configuration Release --output bin
sudo chown -R www-data:www-data .
sudo systemctl daemon-reload
sudo systemctl start prdelbot.service
```

## Setting up service

Create file /lib/systemd/system/prdelbot.service

```
[Unit]
Description=Discord prdel bot
After=network.target

[Service]
User=www-data
WorkingDirectory=/var/app/prdelbot
ExecStart=/usr/bin/dotnet /var/app/prdelbot/bin/prdelbot.dll 5000
Restart=on-failure
StandardOutput=syslog
StandardError=syslog
SyslogIdentifier=prdelbot

[Install]
WantedBy=multi-user.target
```

Then enable it and start it

```bash
sudo systemctl enable prdelbot.service
sudo systemctl start prdelbot.service
```